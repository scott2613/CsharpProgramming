using System.ComponentModel.DataAnnotations;

namespace ComputerStore.Web.Models
{
    // Model dùng cho form Đăng nhập
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập Email hoặc Số điện thoại")]
        [Display(Name = "Tài khoản")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }

    // Model dùng cho form Đăng ký thành viên
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Họ và tên không được để trống")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập Email")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Địa chỉ nhận hàng không được để trống")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải từ 6 ký tự trở lên")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    // Model dùng cho trang Hồ sơ cá nhân
    public class ProfileViewModel
    {
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "Họ và tên không được để trống")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        public string PhoneNumber { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Địa chỉ không được để trống")]
        public string Address { get; set; } = string.Empty;
    }
    // Model Quên mật khẩu & Đặt lại mật khẩu mới
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập Email hoặc Số điện thoại đã đăng ký")]
        [Display(Name = "Tài khoản")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
        [MinLength(6, ErrorMessage = "Mật khẩu mới phải từ 6 ký tự trở lên")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng xác nhận lại mật khẩu mới")]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}