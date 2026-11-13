using ComputerStore.Core.Entities;

namespace ComputerStore.API.Models;

public class CartItem
{
    public Product Product { get; set; } = new();
    public int Quantity { get; set; }
    public decimal TotalPrice => Product.Price * Quantity;
}

public sealed class CartAddViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; } = 1;
    public bool BuyNow { get; set; }
    public string? ReturnUrl { get; set; }
}
