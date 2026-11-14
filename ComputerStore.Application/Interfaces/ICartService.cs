using ComputerStore.Core.Entities;
namespace ComputerStore.Application.Interfaces;
public interface ICartService
{
    Task<List<ShoppingCartItem>> ReadAsync(int userId, CancellationToken ct);
    Task AddAsync(int userId, int productId, int quantity, CancellationToken ct);
    Task RebuyAsync(int userId, int orderId, CancellationToken ct);
    Task UpdateAsync(int userId, int productId, int quantity, CancellationToken ct);
    Task RemoveAsync(int userId, int productId, CancellationToken ct);
    Task ClearAsync(int userId, CancellationToken ct);
}
