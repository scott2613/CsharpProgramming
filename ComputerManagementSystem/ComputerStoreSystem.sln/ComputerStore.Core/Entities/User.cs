namespace ComputerStore.Core.Entities
{
    // Tài khoản người dùng (Khách hàng hoặc Quản trị viên)
    public class User
    {
        // Khóa chính tự tăng
        public int UserId { get; set; }

        // Tên đăng nhập duy nhất (UNIQUE)
        public string Username { get; set; } = string.Empty;

        // Mật khẩu đã băm bảo mật bằng PBKDF2 (ASP.NET Core Identity PasswordHasher)
        public string PasswordHash { get; set; } = string.Empty;

        // Họ và tên người dùng
        public string FullName { get; set; } = string.Empty;

        // Email liên hệ (có thể null, UNIQUE nếu có)
        public string? Email { get; set; }

        // Số điện thoại liên hệ (có thể null, UNIQUE nếu có)
        public string? PhoneNumber { get; set; }

        // Địa chỉ cư trú mặc định
        public string Address { get; set; } = string.Empty;

        // Khóa ngoại phân quyền (1: Admin, 2: Customer)
        public int RoleId { get; set; }
        public Role? Role { get; set; }
    }
}
