using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComputerStore.Core.Entities
{
    // Đại diện cho sản phẩm/máy tính trong kho và trưng bày bán lẻ
    public class Product
    {
        // Khóa chính tự tăng
        public int ProductId { get; set; }

        // Tên máy tính (bao gồm cả hãng, ví dụ: ASUS ROG Strix...)
        public string Name { get; set; } = string.Empty;

        // Đơn giá niêm yết bán lẻ hiện hành (VNĐ)
        public decimal Price { get; set; }

        // Số lượng tồn kho thực tế (ràng buộc >= 0)
        public int StockQuantity { get; set; }

        // Đường dẫn URL hoặc tên file ảnh đại diện
        public string? ImageUrl { get; set; }

        // Thông số kỹ thuật chi tiết (CPU, RAM, VGA, SSD, Màn hình, Pin)
        public string? Specifications { get; set; }

        // Thuộc tính phụ trợ: tự động trỏ về Specifications mà không tạo cột mới trong DB
        [NotMapped]
        public string? Description
        {
            get => Specifications;
            set => Specifications = value;
        }

        // Khóa ngoại liên kết danh mục (Categories)
        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        // Danh sách các dòng chi tiết đơn hàng chứa sản phẩm này
        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
