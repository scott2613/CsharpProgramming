using System;
using System.Collections.Generic;

namespace ComputerStore.Core.Entities
{
    // Chứng từ đơn đặt hàng phát sinh khi khách mua sắm
    public class Order
    {
        // Khóa chính tự tăng (Mã đơn hàng)
        public int OrderId { get; set; }

        // Thời điểm đặt hàng (mặc định UTC)
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        // Họ tên người nhận hàng (lưu vết giao dịch tại thời điểm đặt)
        public string CustomerName { get; set; } = string.Empty;

        // Số điện thoại liên lạc khi giao hàng
        public string CustomerPhone { get; set; } = string.Empty;

        // Địa chỉ nhận máy tính
        public string DeliveryAddress { get; set; } = string.Empty;

        // Tổng tiền đơn hàng (VNĐ)
        public decimal TotalAmount { get; set; }

        // Trạng thái đơn: Chờ xử lý, Đã xác nhận, Đang giao, Hoàn thành, Đã hủy
        public string Status { get; set; } = "Chờ xử lý";

        // Khóa ngoại liên kết tài khoản (Users) - NULL nếu là khách vãng lai
        public int? UserId { get; set; }
        public User? User { get; set; }

        // Danh sách các mặt hàng chi tiết trong đơn
        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
