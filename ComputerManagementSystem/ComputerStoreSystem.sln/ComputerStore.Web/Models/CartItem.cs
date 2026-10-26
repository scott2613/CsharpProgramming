using ComputerStore.Core.Entities;

namespace ComputerStore.Web.Models
{
    public class CartItem
    {
        public Product Product { get; set; } = new();
        public int Quantity { get; set; }
        public decimal TotalPrice => Product.Price * Quantity;
    }
}