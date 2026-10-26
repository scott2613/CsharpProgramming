namespace ComputerStore.Admin.Services
{
    public class ProductDto
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string? Description { get; set; }
        public string? Specifications { get; set; }
        public string? ImageUrl { get; set; }
        public int? CategoryId { get; set; }

        public string? Cpu { get; set; }
        public string? Ram { get; set; }
        public string? Vga { get; set; }

        // Bổ sung thuộc tính hiển thị trạng thái tồn kho chuẩn hóa
        public string StockStatus => StockQuantity > 0 ? $"Còn hàng ({StockQuantity})" : "Hết hàng";
    }
}