using ComputerStore.API.Security;
using ComputerStore.Application.DTOs.Accounts;
using ComputerStore.Application.DTOs.Orders;
using ComputerStore.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComputerStore.API.Controllers;
[ApiController]
[Route("api/orders")]
[Authorize(Roles = StoreRoles.Admin + "," + StoreRoles.Customer)]
[AutoValidateAntiforgeryToken]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class OrdersController(IOrderService orders) : ControllerBase
{
    private int ActorId => AccountIdentity.Id(User);

    [HttpGet, Authorize(Roles = StoreRoles.Admin)]
    public async Task<IActionResult> GetOrders([FromQuery] string? search, [FromQuery] string? customerPhone, CancellationToken ct)
        => Ok(await orders.ListAsync(ActorId, search, customerPhone, ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetOrder(int id, CancellationToken ct)
        => Ok(await orders.GetAsync(ActorId, id, ct));

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] PlaceOrderRequest request, CancellationToken ct)
    {
        var order = await orders.PlaceAsync(ActorId, request, ct);
        return CreatedAtAction(nameof(GetOrder), new { id = order.OrderId },
            new { Message = "Đặt hàng thành công!", order.OrderId, order.TotalAmount, order.Status });
    }

    [HttpPut("{id:int}/status"), Authorize(Roles = StoreRoles.Admin)]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] OrderStatusRequest dto, CancellationToken ct)
    {
        await orders.UpdateStatusAsync(ActorId, id, dto.Status, ct);
        return Ok(new { Message = $"Đã cập nhật trạng thái đơn hàng #{id}." });
    }

    [HttpPut("{id:int}/shipping"), Authorize(Roles = StoreRoles.Admin)]
    public async Task<IActionResult> UpdateOrderShipping(int id, [FromBody] OrderShippingRequest dto, CancellationToken ct)
    {
        await orders.UpdateShippingAsync(ActorId, id, dto, ct);
        return Ok(new { Message = $"Đã cập nhật thông tin giao nhận đơn #{id}." });
    }

    [HttpDelete("{id:int}"), Authorize(Roles = StoreRoles.Admin)]
    public async Task<IActionResult> DeleteOrder(int id, CancellationToken ct)
    {
        await orders.DeleteAsync(ActorId, id, ct);
        return Ok(new { Message = $"Đã xóa đơn hàng #{id}." });
    }
}
