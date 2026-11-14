using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComputerStore.Core.Entities
{
    public class Product
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string? ImageUrl { get; set; }
        public string? Specifications { get; set; }

        // Alias tương thích ngược: tự động trỏ về Specifications mà không tạo cột mới trong DB
        [NotMapped]
        public string? Description
        {
            get => Specifications;
            set => Specifications = value;
        }

        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}