using ComputerStore.Core.Entities;

namespace ComputerStore.Core.Interfaces
{
    public interface IOrderRepository : IRepository<Order>
    {
        Task<IEnumerable<Order>> GetOrdersByUsernameAsync(string username);
        Task<Order?> GetOrderWithDetailsAsync(int orderId);
    }
}