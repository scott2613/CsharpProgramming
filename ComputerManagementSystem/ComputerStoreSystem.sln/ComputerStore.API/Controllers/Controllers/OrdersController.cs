using ComputerStore.API.Security;
using ComputerStore.Application.DTOs.Accounts;
using ComputerStore.Application.DTOs.Orders;
using ComputerStore.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComputerStore.API.Controllers;
[ApiController]
// Tiền tố chung của các API đơn hàng.
[Route("api/orders")]
// Cho phép Admin và Customer vào controller. Các tuyến quản trị yêu cầu riêng vai trò Admin.
[Authorize(Roles = StoreRoles.Admin + "," + StoreRoles.Customer)]
// Kiểm tra mã chống CSRF cho các yêu cầu thay đổi đơn hàng.
[AutoValidateAntiforgeryToken]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class OrdersController(IOrderService orders) : ControllerBase
{
    // Chuyển mã người dùng hiện tại xuống service để kiểm tra quyền thao tác.
    private int ActorId => AccountIdentity.Id(User);

    // Admin xem danh sách đơn hàng và lọc theo từ khóa hoặc số điện thoại.
    [HttpGet, Authorize(Roles = StoreRoles.Admin)]
    public async Task<IActionResult> GetOrders([FromQuery] string? search, [FromQuery] string? customerPhone, CancellationToken ct)
        => Ok(await orders.ListAsync(ActorId, search, customerPhone, ct));

    // Xem đơn theo mã. Service nhận cả mã người gọi để kiểm tra quyền truy cập.
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetOrder(int id, CancellationToken ct)
        => Ok(await orders.GetAsync(ActorId, id, ct));

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] PlaceOrderRequest request, CancellationToken ct)
    {
        // Giao việc xử lý đặt hàng cho service.
        var order = await orders.PlaceAsync(ActorId, request, ct);
        // Trả HTTP 201 kèm đường dẫn đến action xem đơn vừa tạo.
        return CreatedAtAction(nameof(GetOrder), new { id = order.OrderId },
            new { Message = "Đặt hàng thành công!", order.OrderId, order.TotalAmount, order.Status });
    }

    [HttpPut("{id:int}/status"), Authorize(Roles = StoreRoles.Admin)]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] OrderStatusRequest dto, CancellationToken ct)
    {
        // Yêu cầu service cập nhật trạng thái theo mã đơn và trạng thái gửi lên.
        await orders.UpdateStatusAsync(ActorId, id, dto.Status, ct);
        return Ok(new { Message = $"Đã cập nhật trạng thái đơn hàng #{id}." });
    }

    [HttpPut("{id:int}/shipping"), Authorize(Roles = StoreRoles.Admin)]
    public async Task<IActionResult> UpdateOrderShipping(int id, [FromBody] OrderShippingRequest dto, CancellationToken ct)
    {
        // Yêu cầu service lưu thông tin giao nhận theo mã đơn.
        await orders.UpdateShippingAsync(ActorId, id, dto, ct);
        return Ok(new { Message = $"Đã cập nhật thông tin giao nhận đơn #{id}." });
    }

    [HttpDelete("{id:int}"), Authorize(Roles = StoreRoles.Admin)]
    public async Task<IActionResult> DeleteOrder(int id, CancellationToken ct)
    {
        // Yêu cầu service xử lý xóa đơn theo mã.
        await orders.DeleteAsync(ActorId, id, ct);
        return Ok(new { Message = $"Đã xóa đơn hàng #{id}." });
    }
}
