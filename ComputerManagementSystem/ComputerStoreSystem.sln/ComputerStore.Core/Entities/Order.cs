namespace ComputerStore.Core.Entities
{
    public class Order
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string DeliveryAddress { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Chờ xử lý"; // "Chờ xử lý", "Đã xác nhận", "Đang giao", "Hoàn thành", "Đã hủy"

        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}