using ComputerStore.Application.DTOs.Products;
using ComputerStore.Application.Exceptions;
using ComputerStore.Application.Interfaces;
using ComputerStore.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace ComputerStore.Application.Services;

public sealed class ProductQueryService : IProductQueryService
    {
        private readonly IProductReadRepository _repository;

        public ProductQueryService(IProductReadRepository repository)
        {
            _repository = repository;
        }

        public Task<IReadOnlyList<CategorySummaryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
            _repository.GetCategoriesAsync(cancellationToken);

        public Task<IReadOnlyList<ProductDto>> GetAllAsync(
            ProductQueryDto filter, CancellationToken cancellationToken = default)
        {
            return _repository.SearchAsync(Normalize(filter), cancellationToken: cancellationToken);
        }

        public async Task<ProductPageDto> GetPageAsync(
            ProductQueryDto filter, int page = 1, int pageSize = 8,
            CancellationToken cancellationToken = default)
        {
            var normalized = Normalize(filter);
            pageSize = Math.Clamp(pageSize, 1, 50);
            int total = await _repository.CountAsync(normalized, cancellationToken);
            int pages = (int)Math.Ceiling(total / (double)pageSize);
            page = Math.Clamp(page, 1, Math.Max(1, pages));

            var items = total == 0
                ? Array.Empty<ProductDto>()
                : await _repository.SearchAsync(
                    normalized, (page - 1) * pageSize, pageSize, cancellationToken);

            return new ProductPageDto
            {
                Items = items,
                TotalItems = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = pages,
                Filter = normalized
            };
        }

        public Task<IReadOnlyList<ProductDto>> SuggestAsync(
            string? term, CancellationToken cancellationToken = default)
        {
            var filter = Normalize(new ProductQueryDto { Query = term });
            if (filter.Query == null)
                return Task.FromResult<IReadOnlyList<ProductDto>>(Array.Empty<ProductDto>());

            return _repository.SearchAsync(filter, take: 15, cancellationToken: cancellationToken);
        }

        public Task<ProductDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return id <= 0
                ? Task.FromResult<ProductDto?>(null)
                : _repository.GetByIdAsync(id, cancellationToken);
        }

        private static ProductQueryDto Normalize(ProductQueryDto filter)
        {
            ArgumentNullException.ThrowIfNull(filter);
            string? query = Clean(filter.Query, 200, "Từ khóa");
            string? brand = Clean(filter.Brand, 80, "Thương hiệu");
            decimal? min = filter.MinPrice;
            decimal? max = filter.MaxPrice;
            if (min < 0) min = 0;
            if (max < 0) max = 0;
            if (min.HasValue && max.HasValue && min.Value > max.Value)
                (min, max) = (max, min);

            return new ProductQueryDto
            {
                Query = query,
                Brand = brand,
                CategoryId = filter.CategoryId is > 0 ? filter.CategoryId : null,
                MinPrice = min,
                MaxPrice = max,
                SortBy = filter.SortBy?.Trim() switch
                {
                    "price_asc" => "price_asc",
                    "price_desc" => "price_desc",
                    "name_asc" => "name_asc",
                    _ => null
                }
            };
        }

        private static string? Clean(string? value, int maxLength, string field)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            string result = value.Trim();
            if (result.Length > maxLength)
                throw new ArgumentException($"{field} không được vượt quá {maxLength} ký tự.");
            return result;
        }
    }

public sealed class ProductCommandService : IProductCommandService
    {
        private readonly IProductWriteRepository _repository;

        public ProductCommandService(IProductWriteRepository repository)
        {
            _repository = repository;
        }

        public async Task<int> CreateAsync(ProductCreateDto request,
            CancellationToken cancellationToken = default)
        {
            var product = ValidateAndMap(request);
            await EnsureCategoryAsync(product.CategoryId, cancellationToken);
            return await _repository.AddAsync(product, cancellationToken);
        }

        public async Task UpdateAsync(int id, ProductUpdateDto request,
            CancellationToken cancellationToken = default)
        {
            EnsureId(id);
            var product = ValidateAndMap(request);
            if (request.ProductId.HasValue && request.ProductId.Value != id)
                throw Invalid("Mã sản phẩm trong dữ liệu không khớp đường dẫn.");

            var current = await _repository.GetByIdAsync(id, cancellationToken);
            if (current == null)
                throw new ProductOperationException(ProductErrorKind.NotFound, "Không tìm thấy sản phẩm.");

            await EnsureCategoryAsync(product.CategoryId, cancellationToken);
            product.ProductId = id;
            bool changed = await _repository.UpdateAsync(
                product, request.ExpectedStockQuantity!.Value, cancellationToken);
            if (!changed)
                throw new ProductOperationException(ProductErrorKind.Conflict,
                    "Tồn kho đã thay đổi hoặc sản phẩm đã bị xóa. Hãy tải lại danh sách trước khi chỉnh sửa.");
        }

        public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            EnsureId(id);
            return _repository.DeleteIfUnusedAsync(id, cancellationToken);
        }

        private async Task EnsureCategoryAsync(int categoryId, CancellationToken cancellationToken)
        {
            if (!await _repository.CategoryExistsAsync(categoryId, cancellationToken))
                throw Invalid("Danh mục không tồn tại. Vui lòng chọn lại danh mục.");
        }

        private static Product ValidateAndMap(ProductSaveDto request)
        {
            if (request == null) throw Invalid("Dữ liệu sản phẩm không hợp lệ.");
            var errors = new List<ValidationResult>();
            if (!Validator.TryValidateObject(request, new ValidationContext(request), errors, true))
                throw Invalid(string.Join(" ", errors.Select(e => e.ErrorMessage).Distinct()));

            string name = request.Name.Trim();
            if (name.Length == 0) throw Invalid("Tên sản phẩm không được để trống.");
            decimal price = request.Price!.Value;
            if (price < 0 || price > 9999999999999999.99m || decimal.Round(price, 2) != price)
                throw Invalid("Giá phải không âm, tối đa hai chữ số thập phân và nằm trong giới hạn decimal(18,2).");

            string? specifications = Clean(request.Specifications);
            string? description = Clean(request.Description);
            if (specifications != null && description != null && specifications != description)
                throw Invalid("Specifications và Description đang khác nhau; chỉ gửi một nội dung cấu hình thống nhất.");

            return new Product
            {
                Name = name,
                Price = price,
                StockQuantity = request.StockQuantity!.Value,
                CategoryId = request.CategoryId!.Value,
                Specifications = specifications ?? description,
                ImageUrl = NormalizeImage(request.ImageUrl)
            };
        }

        private static string? Clean(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static string? NormalizeImage(string? value)
        {
            string? path = Clean(value);
            if (path == null) return null;
            if (path.Any(char.IsControl) || path.Contains('\\'))
                throw Invalid("Đường dẫn ảnh không hợp lệ.");

            if (path.StartsWith("//", StringComparison.Ordinal))
                throw Invalid("Đường dẫn ảnh phải là HTTP/HTTPS đầy đủ hoặc đường dẫn nội bộ.");

            if (path.StartsWith("/", StringComparison.Ordinal)) return path;

            if (Uri.TryCreate(path, UriKind.Absolute, out var uri))
            {
                if ((uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
                    !string.IsNullOrEmpty(uri.UserInfo))
                    throw Invalid("Ảnh bên ngoài phải dùng HTTP hoặc HTTPS, không chứa thông tin đăng nhập.");

                string normalized = uri.AbsoluteUri;
                if (normalized.Length > 500) throw Invalid("Đường dẫn ảnh tối đa 500 ký tự.");
                return normalized;
            }

            if (path.Contains(':') || path.StartsWith("~", StringComparison.Ordinal) || path.Length >= 500)
                throw Invalid("Đường dẫn ảnh không hợp lệ.");
            return "/" + path;
        }

        private static void EnsureId(int id)
        {
            if (id <= 0) throw Invalid("Mã sản phẩm phải lớn hơn 0.");
        }

        private static ProductOperationException Invalid(string message) =>
            new(ProductErrorKind.InvalidInput, message);
    }
