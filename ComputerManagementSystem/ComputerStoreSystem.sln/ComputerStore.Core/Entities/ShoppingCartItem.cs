namespace ComputerStore.Core.Entities;

// Chỉ lưu lựa chọn. Giá và tồn kho luôn đọc từ Products.
public sealed class ShoppingCartItem
{
    public int UserId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}
