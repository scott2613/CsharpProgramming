namespace ComputerStore.Core.Entities;
public class User
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string Address { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public Role? Role { get; set; }
}
