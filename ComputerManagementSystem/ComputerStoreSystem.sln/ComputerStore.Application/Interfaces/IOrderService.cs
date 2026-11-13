using ComputerStore.Application.DTOs.Orders;
using ComputerStore.Core.Entities;

namespace ComputerStore.Application.Interfaces;
public interface IOrderService
{
    Task<List<Product>> CartProductsAsync(int[] ids, CancellationToken ct);
    Task<OrderSummaryDto> PlaceCartAsync(int actorId, PlaceOrderRequest request, CancellationToken ct);
    Task<OrderSummaryDto> PlaceAsync(int actorId, PlaceOrderRequest request, CancellationToken ct);
    Task<List<OrderSummaryDto>> ListAsync(int actorId, string? search, string? phone, CancellationToken ct);
    Task<OrderSummaryDto> GetAsync(int actorId, int id, CancellationToken ct);
    Task UpdateStatusAsync(int actorId, int id, string status, CancellationToken ct);
    Task UpdateShippingAsync(int actorId, int id, OrderShippingRequest request, CancellationToken ct);
    Task CancelOwnedAsync(int actorId, int id, CancellationToken ct);
    Task DeleteAsync(int actorId, int id, CancellationToken ct);
}
