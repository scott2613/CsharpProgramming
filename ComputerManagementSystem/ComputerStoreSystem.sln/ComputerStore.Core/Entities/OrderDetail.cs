namespace ComputerStore.Core.Entities
{
    // Chi tiết mặt hàng trong đơn (giải thể quan hệ N:N giữa Orders và Products)
    public class OrderDetail
    {
        // Khóa chính tự tăng
        public int OrderDetailId { get; set; }

        // Khóa ngoại trỏ đến đơn hàng
        public int OrderId { get; set; }
        public Order? Order { get; set; }

        // Khóa ngoại trỏ đến sản phẩm được mua
        public int ProductId { get; set; }
        public Product? Product { get; set; }

        // Số lượng đặt mua (phải > 0)
        public int Quantity { get; set; }

        // Đơn giá chốt tại thời điểm mua (Snapshot Pricing, không đổi khi giá Product thay đổi)
        public decimal UnitPrice { get; set; }
    }
}
