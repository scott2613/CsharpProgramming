namespace ComputerStore.Core.Entities;

// UNKNOWN dành cho dữ liệu lịch sử không có bằng chứng xác định phương thức.
public static class PaymentMethods
{
    public const string Cod = "COD";
    public const string Banking = "BANKING";
    public const string Unknown = "UNKNOWN";
    public static string Display(string? method) => method switch
    {
        Cod => "Thanh toán khi nhận hàng (COD)",
        Banking => "Chuyển khoản - nhân viên xác nhận",
        _ => "Chưa ghi nhận"
    };
}
