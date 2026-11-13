using System.ComponentModel.DataAnnotations;
namespace ComputerStore.API.Models;
public class LoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tài khoản"), StringLength(100)] public string Username { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu"), StringLength(128), DataType(DataType.Password)] public string Password { get; set; } = "";
    public bool RememberMe { get; set; }
}
public class RegisterViewModel
{
    [Required, StringLength(100)] public string FullName { get; set; } = "";
    [Required, Phone, StringLength(20)] public string PhoneNumber { get; set; } = "";
    [Required, EmailAddress, StringLength(100)] public string Email { get; set; } = "";
    [Required, StringLength(255)] public string Address { get; set; } = "";
    [Required, StringLength(128, MinimumLength = 8), DataType(DataType.Password)] public string Password { get; set; } = "";
    [Required, Compare(nameof(Password)), DataType(DataType.Password)] public string ConfirmPassword { get; set; } = "";
}
public class ProfileViewModel
{
    public int CustomerId { get; set; }
    public string Username { get; set; } = "";
    [Required, StringLength(100)] public string FullName { get; set; } = "";
    [Required, Phone, StringLength(20)] public string PhoneNumber { get; set; } = "";
    [EmailAddress, StringLength(100)] public string? Email { get; set; }
    [Required, StringLength(255)] public string Address { get; set; } = "";
}
public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tài khoản."), StringLength(100)] public string Identifier { get; set; } = "";
}
public class ForgotPasswordEmailViewModel
{
    [Required, EmailAddress, StringLength(100)] public string Email { get; set; } = "";
}
public class ResetPasswordViewModel
{
    [Required, StringLength(4096)] public string Token { get; set; } = "";
    [Required, StringLength(128, MinimumLength = 8), DataType(DataType.Password)]
    public string NewPassword { get; set; } = "";
    [Required, StringLength(128), Compare(nameof(NewPassword)), DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = "";
}
public class ChangePasswordViewModel
{
    [Required, StringLength(128), DataType(DataType.Password)] public string OldPassword { get; set; } = "";
    [Required, StringLength(128, MinimumLength = 8), DataType(DataType.Password)] public string NewPassword { get; set; } = "";
    [Required, Compare(nameof(NewPassword)), DataType(DataType.Password)] public string ConfirmPassword { get; set; } = "";
}
