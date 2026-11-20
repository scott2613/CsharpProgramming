namespace ComputerStore.Core.Entities
{
    // Giỏ hàng lưu CSDL cho khách hàng đã đăng nhập (giá và tồn kho luôn đọc trực tiếp từ Product)
    public sealed class ShoppingCartItem
    {
        // Khóa chính ghép: Mã người dùng
        public int UserId { get; set; }

        // Khóa chính ghép: Mã sản phẩm
        public int ProductId { get; set; }

        // Số lượng trong giỏ hàng (1 - 999)
        public int Quantity { get; set; }
    }
}
