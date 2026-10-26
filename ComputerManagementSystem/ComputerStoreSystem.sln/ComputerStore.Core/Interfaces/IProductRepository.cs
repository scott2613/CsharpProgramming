using ComputerStore.Core.Entities;

namespace ComputerStore.Core.Interfaces
{
    public interface IProductRepository : IRepository<Product>
    {
        Task<IEnumerable<Product>> GetProductsByCategoryIdAsync(int categoryId);
        Task<IEnumerable<Product>> SearchProductsAsync(string keyword);
        Task<bool> UpdateStockAsync(int productId, int quantityChange);
    }
}