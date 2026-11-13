using ComputerStore.Application.DTOs.Products;
using ComputerStore.Application.Exceptions;
using ComputerStore.Application.Interfaces;
using ComputerStore.Core.Entities;
using ComputerStore.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace ComputerStore.Infrastructure.Repositories;

public sealed class ProductReadRepository : IProductReadRepository
    {
        private readonly ApplicationDbContext _context;

        public ProductReadRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<CategorySummaryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
            await _context.Categories.AsNoTracking().OrderBy(c => c.CategoryId)
                .Select(c => new CategorySummaryDto { CategoryId = c.CategoryId, Name = c.Name }).ToListAsync(cancellationToken);

        public Task<int> CountAsync(ProductQueryDto filter, CancellationToken cancellationToken = default)
        {
            return Filter(filter).CountAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<ProductDto>> SearchAsync(
            ProductQueryDto filter, int skip = 0, int? take = null,
            CancellationToken cancellationToken = default)
        {
            if (skip < 0 || (take.HasValue && take.Value <= 0))
                throw new ArgumentOutOfRangeException(nameof(skip), "Phân trang không hợp lệ.");

            IQueryable<Product> query = filter.SortBy switch
            {
                "price_asc" => Filter(filter).OrderBy(p => p.Price).ThenBy(p => p.ProductId),
                "price_desc" => Filter(filter).OrderByDescending(p => p.Price).ThenBy(p => p.ProductId),
                "name_asc" => Filter(filter).OrderBy(p => p.Name).ThenBy(p => p.ProductId),
                _ => Filter(filter).OrderByDescending(p => p.ProductId)
            };

            if (skip > 0) query = query.Skip(skip);
            if (take.HasValue) query = query.Take(take.Value);
            return await Project(query).ToListAsync(cancellationToken);
        }

        public Task<ProductDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return Project(_context.Products.AsNoTracking().Where(p => p.ProductId == id))
                .SingleOrDefaultAsync(cancellationToken);
        }

        private IQueryable<Product> Filter(ProductQueryDto filter)
        {
            var query = _context.Products.AsNoTracking();
            if (filter.Query != null)
            {
                string keyword = filter.Query.ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(keyword) ||
                    (p.Specifications != null && p.Specifications.ToLower().Contains(keyword)));
            }
            if (filter.CategoryId.HasValue)
                query = query.Where(p => p.CategoryId == filter.CategoryId.Value);
            // Compatibility with the old API: brand is matched against product name.
            if (filter.Brand != null)
            {
                string brand = filter.Brand.ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(brand));
            }
            if (filter.MinPrice.HasValue)
                query = query.Where(p => p.Price >= filter.MinPrice.Value);
            if (filter.MaxPrice.HasValue)
                query = query.Where(p => p.Price <= filter.MaxPrice.Value);
            return query;
        }

        private static IQueryable<ProductDto> Project(IQueryable<Product> query)
        {
            return query.Select(p => new ProductDto
            {
                ProductId = p.ProductId,
                Name = p.Name,
                Price = p.Price,
                StockQuantity = p.StockQuantity,
                ImageUrl = p.ImageUrl,
                Specifications = p.Specifications,
                CategoryId = p.CategoryId,
                Category = p.Category == null ? null : new CategorySummaryDto
                {
                    CategoryId = p.Category.CategoryId,
                    Name = p.Category.Name
                }
            });
        }
    }

public sealed class ProductWriteRepository : IProductWriteRepository
    {
        private readonly ApplicationDbContext _context;

        public ProductWriteRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            _context.Products.AsNoTracking().SingleOrDefaultAsync(p => p.ProductId == id, cancellationToken);

        public Task<bool> CategoryExistsAsync(int categoryId, CancellationToken cancellationToken = default) =>
            _context.Categories.AnyAsync(c => c.CategoryId == categoryId, cancellationToken);

        public async Task<int> AddAsync(Product product, CancellationToken cancellationToken = default)
        {
            try
            {
                _context.Products.Add(product);
                await _context.SaveChangesAsync(cancellationToken);
                return product.ProductId;
            }
            catch (Exception ex) when (IsConflict(ex))
            {
                throw Conflict("Dữ liệu liên quan đã thay đổi. Vui lòng kiểm tra lại danh mục và thử lại.");
            }
        }

        public async Task<bool> UpdateAsync(Product product, int expectedStockQuantity,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Compare and update in one SQL command. This does not version every field
                // or fix old order writers that later overwrite a stale stock value.
                int affected = await _context.Products
                    .Where(p => p.ProductId == product.ProductId && p.StockQuantity == expectedStockQuantity)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(p => p.Name, product.Name)
                        .SetProperty(p => p.Price, product.Price)
                        .SetProperty(p => p.StockQuantity, product.StockQuantity)
                        .SetProperty(p => p.Specifications, product.Specifications)
                        .SetProperty(p => p.ImageUrl, product.ImageUrl)
                        .SetProperty(p => p.CategoryId, product.CategoryId), cancellationToken);
                return affected == 1;
            }
            catch (Exception ex) when (IsConflict(ex))
            {
                throw Conflict("Sản phẩm đang được thay đổi hoặc danh mục không còn hợp lệ. Vui lòng tải lại.");
            }
        }

        public async Task DeleteIfUnusedAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable, cancellationToken);

                bool exists = await _context.Products.AnyAsync(p => p.ProductId == id, cancellationToken);
                if (!exists)
                    throw new ProductOperationException(ProductErrorKind.NotFound, "Không tìm thấy sản phẩm.");

                bool used = await _context.OrderDetails.AnyAsync(d => d.ProductId == id, cancellationToken);
                if (used)
                    throw Conflict("Không thể xóa sản phẩm đã xuất hiện trong đơn hàng.");

                int affected = await _context.Products
                    .Where(p => p.ProductId == id && !_context.OrderDetails.Any(d => d.ProductId == p.ProductId))
                    .ExecuteDeleteAsync(cancellationToken);
                if (affected != 1) throw Conflict("Sản phẩm đã thay đổi. Vui lòng tải lại danh sách.");

                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex) when (IsConflict(ex))
            {
                throw Conflict("Sản phẩm đang được sử dụng hoặc cập nhật. Vui lòng tải lại trước khi xóa.");
            }
        }

        private static bool IsConflict(Exception exception)
        {
            for (Exception? current = exception; current != null; current = current.InnerException)
            {
                if (current is SqlException sql && sql.Number is 547 or 1205 or 1222 or 3960)
                    return true;
            }
            return false;
        }

        private static ProductOperationException Conflict(string message) =>
            new(ProductErrorKind.Conflict, message);
    }
