using ComputerStore.Application.DTOs.Products;
using ComputerStore.Core.Entities;

namespace ComputerStore.Application.Interfaces;

public interface IProductQueryService
    {
        Task<IReadOnlyList<CategorySummaryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ProductDto>> GetAllAsync(
            ProductQueryDto filter, CancellationToken cancellationToken = default);

        Task<ProductPageDto> GetPageAsync(
            ProductQueryDto filter, int page = 1, int pageSize = 8,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ProductDto>> SuggestAsync(
            string? term, CancellationToken cancellationToken = default);

        Task<ProductDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    }

public interface IProductCommandService
    {
        Task<int> CreateAsync(ProductCreateDto request, CancellationToken cancellationToken = default);
        Task UpdateAsync(int id, ProductUpdateDto request, CancellationToken cancellationToken = default);
        Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    }

public interface IProductReadRepository
    {
        Task<IReadOnlyList<CategorySummaryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
        Task<int> CountAsync(ProductQueryDto filter, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ProductDto>> SearchAsync(
            ProductQueryDto filter,
            int skip = 0,
            int? take = null,
            CancellationToken cancellationToken = default);

        Task<ProductDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    }

public interface IProductWriteRepository
    {
        Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<bool> CategoryExistsAsync(int categoryId, CancellationToken cancellationToken = default);
        Task<int> AddAsync(Product product, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(Product product, int expectedStockQuantity,
            CancellationToken cancellationToken = default);
        Task DeleteIfUnusedAsync(int id, CancellationToken cancellationToken = default);
    }
