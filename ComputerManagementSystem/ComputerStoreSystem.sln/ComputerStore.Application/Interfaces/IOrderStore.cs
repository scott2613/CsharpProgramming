using ComputerStore.Application.DTOs.Orders;
using ComputerStore.Core.Entities;

namespace ComputerStore.Application.Interfaces;
public interface IOrderTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct);
}
public interface IOrderStore
{
    Task<bool> IsAdminAsync(int userId, CancellationToken ct);
    Task<bool> IsAccountAsync(int userId, CancellationToken ct);
    Task<List<Product>> CartProductsAsync(int[] ids, CancellationToken ct);
    Task<List<OrderSummaryDto>> ListAsync(string? search, string? phone, CancellationToken ct);
    Task<OrderSummaryDto?> ReadAsync(int id, int actorId, bool admin, CancellationToken ct);
    Task<IOrderTransaction> BeginAsync(CancellationToken ct);
    Task<Product?> LockProductAsync(int id, CancellationToken ct);
    Task<Order?> LockOrderAsync(int id, CancellationToken ct);
    void Add(Order order);
    void Delete(Order order);
    Task SaveAsync(CancellationToken ct);
    Task<bool> LockCartOwnerAsync(int userId, CancellationToken ct);
    Task<List<ShoppingCartItem>> ReadCartAsync(int userId, CancellationToken ct);
    Task<List<ShoppingCartItem>> ReadCartForUpdateAsync(int userId, CancellationToken ct);
    void AddCartItem(ShoppingCartItem item);
    void RemoveCartItems(IEnumerable<ShoppingCartItem> items);
}
