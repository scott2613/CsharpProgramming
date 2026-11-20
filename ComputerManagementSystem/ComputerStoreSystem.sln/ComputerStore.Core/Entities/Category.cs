using System.Collections.Generic;

namespace ComputerStore.Core.Entities
{
    // Danh mục phân loại máy tính (Laptop Gaming, PC, All-in-One...)
    public class Category
    {
        // Khóa chính tự tăng
        public int CategoryId { get; set; }

        // Tên danh mục duy nhất
        public string Name { get; set; } = string.Empty;

        // Mô tả định hướng tiêu dùng
        public string? Description { get; set; }

        // Danh sách các máy tính thuộc danh mục này
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
