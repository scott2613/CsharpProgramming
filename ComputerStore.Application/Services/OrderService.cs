using System.ComponentModel.DataAnnotations;
using ComputerStore.Application.DTOs.Orders;
using ComputerStore.Application.Exceptions;
using ComputerStore.Application.Interfaces;
using ComputerStore.Core.Entities;

namespace ComputerStore.Application.Services;
public sealed class OrderService(IOrderStore store) : IOrderService
{
    private static void Validate(object value)
    {
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(value, new ValidationContext(value), errors, true))
            throw new OrderOperationException(string.Join(" ", errors.Select(e => e.ErrorMessage)));
    }
    private async Task AdminAsync(int actorId, CancellationToken ct)
    {
        if (!await store.IsAdminAsync(actorId, ct))
            throw new OrderOperationException("Chức năng này dành cho Admin.", 403);
    }
    public Task<List<Product>> CartProductsAsync(int[] ids, CancellationToken ct) =>
        store.CartProductsAsync(ids.Where(i => i > 0).Distinct().Take(100).ToArray(), ct);

    public Task<OrderSummaryDto> PlaceAsync(int actorId, PlaceOrderRequest request, CancellationToken ct) =>
        PlaceCoreAsync(actorId, request, false, ct);
    public Task<OrderSummaryDto> PlaceCartAsync(int actorId, PlaceOrderRequest request, CancellationToken ct) =>
        PlaceCoreAsync(actorId, request, true, ct);

    private async Task<OrderSummaryDto> PlaceCoreAsync(int actorId, PlaceOrderRequest request, bool fromCart, CancellationToken ct)
    {
        if (!await store.IsAccountAsync(actorId, ct))
            throw new OrderOperationException("Vui lòng đăng nhập lại.", 401);
        await using var transaction = await store.BeginAsync(ct);
        List<ShoppingCartItem>? purchasedCart = null;
        if (fromCart)
        {
            if (!await store.LockCartOwnerAsync(actorId, ct))
                throw new OrderOperationException("Tài khoản không còn tồn tại.", 401);
            purchasedCart = await store.ReadCartForUpdateAsync(actorId, ct);
            if (purchasedCart.Count == 0)
                throw new OrderOperationException("Giỏ hàng đã trống. Hãy kiểm tra Đơn hàng nếu bạn vừa đặt thành công.", 409);
            request.OrderDetails = purchasedCart.Select(x => new OrderLineRequest
            { ProductId = x.ProductId, Quantity = x.Quantity }).ToList();
        }
        Validate(request);
        if (request.OrderDetails == null || request.OrderDetails.Count is < 1 or > 100)
            throw new OrderOperationException("Giỏ hàng phải có từ 1 đến 100 sản phẩm.");
        foreach (var line in request.OrderDetails)
        {
            if (line == null) throw new OrderOperationException("Dòng sản phẩm không hợp lệ.");
            Validate(line);
        }
        // Gộp ID trùng rồi kiểm tra tổng, không để nhiều dòng vượt giới hạn.
        var lines = request.OrderDetails.GroupBy(d => d.ProductId)
            .Select(g => new { Id = g.Key, Quantity = g.Sum(x => (long)x.Quantity) })
            .OrderBy(d => d.Id).ToList();
        if (lines.Any(d => d.Quantity > 999))
            throw new OrderOperationException("Mỗi sản phẩm chỉ được đặt tối đa 999 máy.");
        var name = request.CustomerName.Trim();
        var phone = request.CustomerPhone.Trim();
        var address = (!string.IsNullOrWhiteSpace(request.DeliveryAddress)
            ? request.DeliveryAddress : request.CustomerAddress)?.Trim();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(phone) ||
            string.IsNullOrWhiteSpace(address) || address.Length > 255)
            throw new OrderOperationException("Vui lòng nhập đủ họ tên, số điện thoại và địa chỉ (tối đa 255 ký tự).");
        var method = request.PaymentMethod?.Trim().ToUpperInvariant();
        var payment = method switch
        {
            "COD" => "Thanh toán khi nhận hàng (COD)",
            "BANKING" => "Chuyển khoản - chờ nhân viên xác nhận",
            _ => throw new OrderOperationException("Phương thức này chưa được hỗ trợ. Hãy chọn COD hoặc chuyển khoản.")
        };

        var order = new Order
        {
            UserId = actorId,
            CustomerName = name,
            CustomerPhone = phone,
            DeliveryAddress = $"{address} [PTTT: {payment}]",
            // Giữ cách hiển thị giờ Việt Nam của dữ liệu cũ; chưa đổi quy ước toàn DB.
            OrderDate = DateTime.UtcNow.AddHours(7),
            Status = OrderStatuses.Pending
        };
        foreach (var line in lines)
        {
            var product = await store.LockProductAsync(line.Id, ct)
                ?? throw new OrderOperationException($"Sản phẩm #{line.Id} không còn tồn tại.", 409);
            if (product.Price < 0 || product.StockQuantity < line.Quantity)
                throw new OrderOperationException($"Sản phẩm '{product.Name}' chỉ còn {Math.Max(0, product.StockQuantity)} máy hoặc giá chưa hợp lệ.", 409);
            var quantity = (int)line.Quantity;
            order.TotalAmount += product.Price * quantity;
            if (order.TotalAmount > 9999999999999999.99m)
                throw new OrderOperationException("Tổng tiền vượt giới hạn xử lý.");
            order.OrderDetails.Add(new OrderDetail
            {
                ProductId = product.ProductId,
                Quantity = quantity,
                UnitPrice = product.Price
            });
            product.StockQuantity -= quantity;
        }
        if (request.ExpectedTotal.HasValue && request.ExpectedTotal.Value != order.TotalAmount)
            throw new OrderOperationException("Giá sản phẩm đã thay đổi. Vui lòng kiểm tra tổng tiền mới rồi đặt lại.", 409);
        store.Add(order);
        if (purchasedCart != null) store.RemoveCartItems(purchasedCart);
        // Ghi đơn, trừ kho và xóa giỏ cùng commit; lỗi ở bất kỳ bước nào sẽ rollback.
        await store.SaveAsync(ct);
        await transaction.CommitAsync(ct);
        return new OrderSummaryDto
        {
            OrderId = order.OrderId,
            UserId = actorId,
            TotalAmount = order.TotalAmount,
            Status = order.Status,
            OrderDate = order.OrderDate,
            CustomerName = name,
            CustomerPhone = phone,
            DeliveryAddress = order.DeliveryAddress
        };
    }
    public async Task<List<OrderSummaryDto>> ListAsync(int actorId, string? search, string? phone, CancellationToken ct)
    {
        await AdminAsync(actorId, ct);
        return await store.ListAsync(search, phone, ct);
    }
    public async Task<OrderSummaryDto> GetAsync(int actorId, int id, CancellationToken ct)
    {
        if (actorId <= 0) throw new OrderOperationException("Vui lòng đăng nhập.", 401);
        var admin = await store.IsAdminAsync(actorId, ct);
        return await store.ReadAsync(id, actorId, admin, ct)
            ?? throw new OrderOperationException("Không tìm thấy đơn hàng.", 404);
    }
    private async Task RestoreStockAsync(Order order, CancellationToken ct)
    {
        foreach (var line in order.OrderDetails.GroupBy(d => d.ProductId).OrderBy(g => g.Key))
        {
            var qty = line.Sum(d => (long)d.Quantity);
            if (qty <= 0) throw new OrderOperationException("Đơn hàng cũ có số lượng không hợp lệ; cần kiểm tra dữ liệu.", 409);
            var p = await store.LockProductAsync(line.Key, ct)
                ?? throw new OrderOperationException("Thiếu sản phẩm để hoàn kho. Cần kiểm tra dữ liệu.", 409);
            if (qty > int.MaxValue - (long)p.StockQuantity)
                throw new OrderOperationException("Số lượng hoàn kho vượt giới hạn.", 409);
            p.StockQuantity += (int)qty;
        }
    }
    public async Task UpdateStatusAsync(int actorId, int id, string status, CancellationToken ct)
    {
        await AdminAsync(actorId, ct);
        var next = OrderStatuses.Normalize(status);
        if (next is not (OrderStatuses.Pending or OrderStatuses.Confirmed or OrderStatuses.Shipping or
            OrderStatuses.Completed or OrderStatuses.Cancelled))
            throw new OrderOperationException("Trạng thái đơn hàng không hợp lệ.");
        await using var transaction = await store.BeginAsync(ct);
        var order = await store.LockOrderAsync(id, ct)
            ?? throw new OrderOperationException("Không tìm thấy đơn hàng.", 404);
        var current = OrderStatuses.Normalize(order.Status);
        if (!OrderStatuses.CanMove(current, next))
            throw new OrderOperationException($"Không thể chuyển từ '{order.Status}' sang '{next}'. Đơn đã hủy/hoàn thành không được mở lại.", 409);
        if (next == OrderStatuses.Cancelled && current != OrderStatuses.Cancelled)
            await RestoreStockAsync(order, ct);
        order.Status = next;
        await store.SaveAsync(ct);
        await transaction.CommitAsync(ct);
    }
    public async Task UpdateShippingAsync(int actorId, int id, OrderShippingRequest request, CancellationToken ct)
    {
        await AdminAsync(actorId, ct);
        Validate(request);
        if (request.CustomerName == null && request.CustomerPhone == null && request.DeliveryAddress == null)
            throw new OrderOperationException("Chưa có thông tin cần cập nhật.");
        if ((request.CustomerName != null && string.IsNullOrWhiteSpace(request.CustomerName)) ||
            (request.CustomerPhone != null && string.IsNullOrWhiteSpace(request.CustomerPhone)) ||
            (request.DeliveryAddress != null && string.IsNullOrWhiteSpace(request.DeliveryAddress)))
            throw new OrderOperationException("Thông tin nhận hàng không được để trống.");
        await using var transaction = await store.BeginAsync(ct);
        var order = await store.LockOrderAsync(id, ct)
            ?? throw new OrderOperationException("Không tìm thấy đơn hàng.", 404);
        if (OrderStatuses.Normalize(order.Status) is OrderStatuses.Completed or OrderStatuses.Cancelled)
            throw new OrderOperationException("Không sửa thông tin giao nhận của đơn đã kết thúc.", 409);
        if (request.CustomerName != null) order.CustomerName = request.CustomerName.Trim();
        if (request.CustomerPhone != null) order.CustomerPhone = request.CustomerPhone.Trim();
        if (request.DeliveryAddress != null) order.DeliveryAddress = request.DeliveryAddress.Trim();
        await store.SaveAsync(ct);
        await transaction.CommitAsync(ct);
    }
    public async Task CancelOwnedAsync(int actorId, int id, CancellationToken ct)
    {
        if (actorId <= 0 || !await store.IsAccountAsync(actorId, ct))
            throw new OrderOperationException("Vui lòng đăng nhập lại.", 401);
        await using var transaction = await store.BeginAsync(ct);
        var order = await store.LockOrderAsync(id, ct);
        if (order == null || order.UserId != actorId)
            throw new OrderOperationException("Không tìm thấy đơn hàng.", 404);
        var status = OrderStatuses.Normalize(order.Status);
        if (status != OrderStatuses.Cancelled)
        {
            if (status != OrderStatuses.Pending)
                throw new OrderOperationException("Đơn đã được tiếp nhận. Vui lòng liên hệ cửa hàng để hỗ trợ hủy.", 409);
            await RestoreStockAsync(order, ct);
            order.Status = OrderStatuses.Cancelled;
            await store.SaveAsync(ct);
        }
        await transaction.CommitAsync(ct);
    }
    public async Task DeleteAsync(int actorId, int id, CancellationToken ct)
    {
        await AdminAsync(actorId, ct);
        await using var transaction = await store.BeginAsync(ct);
        var order = await store.LockOrderAsync(id, ct)
            ?? throw new OrderOperationException("Không tìm thấy đơn hàng.", 404);
        var status = OrderStatuses.Normalize(order.Status);
        if (status == OrderStatuses.Completed)
            throw new OrderOperationException("Giữ đơn đã hoàn thành để đối chiếu lịch sử bán hàng.", 409);
        if (status is not (OrderStatuses.Pending or OrderStatuses.Confirmed or OrderStatuses.Shipping or OrderStatuses.Cancelled))
            throw new OrderOperationException("Trạng thái cũ chưa được nhận diện; cần kiểm tra trước khi xóa.", 409);
        if (status != OrderStatuses.Cancelled) await RestoreStockAsync(order, ct);
        store.Delete(order);
        await store.SaveAsync(ct);
        await transaction.CommitAsync(ct);
    }
}
