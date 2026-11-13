using System.ComponentModel.DataAnnotations;

namespace ComputerStore.Application.DTOs.Orders;

public sealed class OrderLineRequest
{
    [Range(1, int.MaxValue)] public int ProductId { get; set; }
    [Range(1, 999)] public int Quantity { get; set; }
}

public sealed class PlaceOrderRequest
{
    [Required, StringLength(100)] public string CustomerName { get; set; } = "";
    [Required, Phone, StringLength(20)] public string CustomerPhone { get; set; } = "";
    [StringLength(255)] public string? DeliveryAddress { get; set; }
    [StringLength(255)] public string? CustomerAddress { get; set; }
    public string PaymentMethod { get; set; } = "COD";
    // So sánh giá đã hiển thị với giá DB; không dùng làm giá thanh toán.
    public decimal? ExpectedTotal { get; set; }
    [Required, MinLength(1), MaxLength(100)]
    public List<OrderLineRequest> OrderDetails { get; set; } = new();
}

public sealed class OrderStatusRequest
{
    [Required, StringLength(30)] public string Status { get; set; } = "";
}
public sealed class OrderShippingRequest
{
    [StringLength(100)] public string? CustomerName { get; set; }
    [Phone, StringLength(20)] public string? CustomerPhone { get; set; }
    [StringLength(500)] public string? DeliveryAddress { get; set; }
}
public sealed class OrderSummaryDto
{
    public int OrderId { get; set; }
    public int? UserId { get; set; }
    public string CustomerName { get; set; } = "";
    public string CustomerPhone { get; set; } = "";
    public string DeliveryAddress { get; set; } = "";
    public string FinalAddress => DeliveryAddress;
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "";
    public List<OrderLineDto> OrderDetails { get; set; } = new();
}
public sealed class OrderLineDto
{
    public int OrderDetailId { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice => UnitPrice * Quantity;
}
public static class OrderStatuses
{
    public const string Pending = "Chờ xử lý";
    public const string Confirmed = "Đã xác nhận";
    public const string Shipping = "Đang giao";
    public const string Completed = "Đã hoàn thành";
    public const string Cancelled = "Đã hủy";
    public static string Normalize(string? value) => value?.Trim() switch
    {
        "Hoàn thành" => Completed,
        "Đang giao hàng" => Shipping,
        var x => x ?? ""
    };
    public static bool CanMove(string current, string next) => current == next || (current switch
    {
        Pending => next is Confirmed or Shipping or Completed or Cancelled,
        Confirmed => next is Shipping or Completed or Cancelled,
        Shipping => next is Completed or Cancelled,
        _ => false
    });
}
