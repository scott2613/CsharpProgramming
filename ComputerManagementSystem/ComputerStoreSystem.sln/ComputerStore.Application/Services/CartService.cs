using ComputerStore.Application.Exceptions;
using ComputerStore.Application.Interfaces;
using ComputerStore.Core.Entities;

namespace ComputerStore.Application.Services;
public sealed class CartService(IOrderStore store) : ICartService
{
    private async Task CheckAccountAsync(int userId, CancellationToken ct)
    {
        if (userId <= 0 || !await store.IsAccountAsync(userId, ct))
            throw new OrderOperationException("Vui lòng đăng nhập lại.", 401);
    }
    public async Task<List<ShoppingCartItem>> ReadAsync(int userId, CancellationToken ct)
    {
        await CheckAccountAsync(userId, ct);
        return await store.ReadCartAsync(userId, ct);
    }
    private async Task MutateAsync(int userId, Func<List<ShoppingCartItem>, Task> change, CancellationToken ct)
    {
        await CheckAccountAsync(userId, ct);
        await using var transaction = await store.BeginAsync(ct);
        if (!await store.LockCartOwnerAsync(userId, ct))
            throw new OrderOperationException("Tài khoản không còn tồn tại.", 401);
        var items = await store.ReadCartForUpdateAsync(userId, ct);
        await change(items);
        await store.SaveAsync(ct);
        await transaction.CommitAsync(ct);
    }
    private static void CheckInput(int productId, int quantity, bool allowZero)
    {
        if (productId <= 0 || quantity < (allowZero ? 0 : 1) || quantity > 999)
            throw new OrderOperationException("Sản phẩm hoặc số lượng không hợp lệ (tối đa 999 máy).");
    }
    public Task AddAsync(int userId, int productId, int quantity, CancellationToken ct)
    {
        CheckInput(productId, quantity, false);
        return MutateAsync(userId, items => AddItemAsync(userId, items, productId, quantity, ct), ct);
    }

    public Task RebuyAsync(int userId, int orderId, CancellationToken ct) =>
        MutateAsync(userId, async items =>
        {
            var order = await store.ReadAsync(orderId, userId, false, ct)
                ?? throw new OrderOperationException("Không tìm thấy đơn hàng của bạn.", 404);
            if (order.OrderDetails.Count == 0)
                throw new OrderOperationException("Đơn hàng không có sản phẩm để mua lại.");
            foreach (var group in order.OrderDetails.GroupBy(x => x.ProductId).OrderBy(x => x.Key))
            {
                var quantity = group.Sum(x => (long)x.Quantity);
                if (quantity is < 1 or > 999)
                    throw new OrderOperationException("Số lượng mua lại không hợp lệ.");
                await AddItemAsync(userId, items, group.Key, (int)quantity, ct);
            }
        }, ct);

    private async Task AddItemAsync(int userId, List<ShoppingCartItem> items,
        int productId, int quantity, CancellationToken ct)
    {
        var existing = items.FirstOrDefault(x => x.ProductId == productId);
        if (existing == null && items.Count >= 100)
            throw new OrderOperationException("Giỏ hàng tối đa 100 sản phẩm khác nhau.");
        var product = await store.LockProductAsync(productId, ct)
            ?? throw new OrderOperationException("Sản phẩm không còn bán.", 409);
        var total = (long)(existing?.Quantity ?? 0) + quantity;
        var maximum = Math.Min(999, product.StockQuantity);
        if (maximum <= 0 || total > maximum)
            throw new OrderOperationException($"'{product.Name}' chỉ có thể chọn tối đa {Math.Max(0, maximum)} máy theo tồn kho hiện tại.", 409);
        if (existing is null)
        {
            var added = new ShoppingCartItem { UserId = userId, ProductId = productId, Quantity = (int)total };
            store.AddCartItem(added);
            items.Add(added);
        }
        else existing.Quantity = (int)total;
    }

    public Task UpdateAsync(int userId, int productId, int quantity, CancellationToken ct)
    {
        CheckInput(productId, quantity, true);
        return MutateAsync(userId, async items =>
        {
            var item = items.FirstOrDefault(x => x.ProductId == productId);
            if (item == null) return;
            if (quantity == 0) { store.RemoveCartItems(new[] { item }); return; }
            var product = await store.LockProductAsync(productId, ct)
                ?? throw new OrderOperationException("Sản phẩm không còn bán.", 409);
            if (quantity > product.StockQuantity)
                throw new OrderOperationException($"'{product.Name}' chỉ còn {Math.Max(0, product.StockQuantity)} máy.", 409);
            item.Quantity = quantity;
        }, ct);
    }
    public Task RemoveAsync(int userId, int productId, CancellationToken ct)
    {
        CheckInput(productId, 0, true);
        return MutateAsync(userId, items =>
        {
            store.RemoveCartItems(items.Where(x => x.ProductId == productId));
            return Task.CompletedTask;
        }, ct);
    }
    public Task ClearAsync(int userId, CancellationToken ct) => MutateAsync(userId, items =>
    {
        store.RemoveCartItems(items);
        return Task.CompletedTask;
    }, ct);
}
