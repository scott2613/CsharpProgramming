using System.ComponentModel.DataAnnotations;

namespace ComputerStore.Core.DTOs
{
    public class ProductCreateDto
    {
        [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Range(0, double.MaxValue, ErrorMessage = "Giá bán phải lớn hơn hoặc bằng 0")]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn kho không hợp lệ")]
        public int StockQuantity { get; set; }

        public string ImageUrl { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn danh mục sản phẩm")]
        public int CategoryId { get; set; }
    }
}