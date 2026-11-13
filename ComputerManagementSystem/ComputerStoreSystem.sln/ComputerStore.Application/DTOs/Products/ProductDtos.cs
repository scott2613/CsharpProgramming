using System.ComponentModel.DataAnnotations;

namespace ComputerStore.Application.DTOs.Products;

public sealed class ProductQueryDto
    {
        public string? Query { get; set; }
        public int? CategoryId { get; set; }
        public string? Brand { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? SortBy { get; set; }
    }

    public sealed class CategorySummaryDto
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public sealed class ProductDto
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string? ImageUrl { get; set; }
        public string? Specifications { get; set; }
        public string? Description => Specifications;
        public int CategoryId { get; set; }
        public CategorySummaryDto? Category { get; set; }
    }

    public sealed class ProductPageDto
    {
        public IReadOnlyList<ProductDto> Items { get; set; } = Array.Empty<ProductDto>();
        public int TotalItems { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 8;
        public int TotalPages { get; set; }
        public ProductQueryDto Filter { get; set; } = new();
    }

public abstract class ProductSaveDto
    {
        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm.")]
        [StringLength(200, ErrorMessage = "Tên sản phẩm tối đa 200 ký tự.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập giá sản phẩm.")]
        public decimal? Price { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số lượng tồn kho.")]
        [Range(0, int.MaxValue, ErrorMessage = "Tồn kho không được âm.")]
        public int? StockQuantity { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn danh mục.")]
        [Range(1, int.MaxValue, ErrorMessage = "Mã danh mục không hợp lệ.")]
        public int? CategoryId { get; set; }

        [StringLength(20000, ErrorMessage = "Cấu hình tối đa 20.000 ký tự.")]
        public string? Specifications { get; set; }

        // Accept the old Admin field name while clients migrate to Specifications.
        [StringLength(20000, ErrorMessage = "Mô tả tối đa 20.000 ký tự.")]
        public string? Description { get; set; }

        [StringLength(500, ErrorMessage = "Đường dẫn ảnh tối đa 500 ký tự.")]
        public string? ImageUrl { get; set; }
    }

    public sealed class ProductCreateDto : ProductSaveDto
    {
    }

    public sealed class ProductUpdateDto : ProductSaveDto
    {
        public int? ProductId { get; set; }

        [Required(ErrorMessage = "Thiếu tồn kho gốc. Vui lòng tải lại sản phẩm trước khi lưu.")]
        [Range(0, int.MaxValue, ErrorMessage = "Tồn kho gốc không hợp lệ.")]
        public int? ExpectedStockQuantity { get; set; }
    }
