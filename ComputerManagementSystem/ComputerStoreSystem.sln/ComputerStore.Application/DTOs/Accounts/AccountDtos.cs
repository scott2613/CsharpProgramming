using System.ComponentModel.DataAnnotations;

namespace ComputerStore.Application.DTOs.Accounts;

public static class StoreRoles
{
    public const string Admin = "Admin";
    public const string Customer = "Customer";
    public static bool IsAllowed(string? name) => name is Admin or Customer;
}

public sealed class AccountDto
{
    public int UserId { get; set; }
    public int CustomerId => UserId;
    public string Username { get; set; } = "";
    public string FullName { get; set; } = "";
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string Address { get; set; } = "";
    public int RoleId { get; set; }
    public string RoleName { get; set; } = "";
    public string Role => RoleName;
}

// Chỉ dùng nội bộ giữa service và lớp xác thực; không trả Stamp cho client.
public sealed record AuthenticatedAccount(AccountDto Account, string Stamp);

public sealed class AccountLoginDto
{
    [Required, StringLength(100)] public string Username { get; set; } = "";
    [Required, StringLength(128)] public string Password { get; set; } = "";
    public bool RememberMe { get; set; }
    public bool AdminClient { get; set; }
}

public class AccountRegisterDto
{
    [StringLength(50)] public string? Username { get; set; }
    [Required, StringLength(100)] public string FullName { get; set; } = "";
    [EmailAddress, StringLength(100)] public string? Email { get; set; }
    [Phone, StringLength(20)] public string? PhoneNumber { get; set; }
    [StringLength(255)] public string Address { get; set; } = "";
    [Required, StringLength(128, MinimumLength = 8)] public string Password { get; set; } = "";
}

public sealed class AdminCreateAccountDto : AccountRegisterDto
{
    [Range(1, int.MaxValue)] public int RoleId { get; set; } = 2;
}

public sealed class AdminUpdateAccountDto
{
    [StringLength(50)] public string? Username { get; set; }
    [StringLength(100)] public string? FullName { get; set; }
    [EmailAddress, StringLength(100)] public string? Email { get; set; }
    [Phone, StringLength(20)] public string? PhoneNumber { get; set; }
    [StringLength(255)] public string? Address { get; set; }
    [Range(1, int.MaxValue)] public int? RoleId { get; set; }
}

public sealed class AccountProfileUpdateDto
{
    [Required, StringLength(100)] public string FullName { get; set; } = "";
    [Required, Phone, StringLength(20)] public string PhoneNumber { get; set; } = "";
    [EmailAddress, StringLength(100)] public string? Email { get; set; }
    [Required, StringLength(255)] public string Address { get; set; } = "";
}

public sealed class AccountPasswordChangeDto
{
    [Required, StringLength(128)] public string OldPassword { get; set; } = "";
    [Required, StringLength(128, MinimumLength = 8)] public string NewPassword { get; set; } = "";
}

public sealed class AdminPasswordResetDto
{
    [Required, StringLength(128, MinimumLength = 8)] public string NewPassword { get; set; } = "";
}

public sealed class CustomerOrderHistoryDto
{
    public int OrderId { get; set; }
    public string CustomerName { get; set; } = "";
    public string CustomerPhone { get; set; } = "";
    public string DeliveryAddress { get; set; } = "";
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "";
    public List<CustomerOrderDetailDto> OrderDetails { get; set; } = new();
}

public sealed class CustomerOrderDetailDto
{
    public int OrderDetailId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice => Quantity * UnitPrice;
}
