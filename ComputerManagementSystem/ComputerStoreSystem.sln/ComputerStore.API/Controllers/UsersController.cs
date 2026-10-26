using ComputerStore.Core.Entities;
using ComputerStore.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace ComputerStore.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        private static string ComputeSha256Hash(string rawData)
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            var builder = new StringBuilder();
            foreach (var b in bytes)
            {
                builder.Append(b.ToString("x2"));
            }
            return builder.ToString();
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] string? keyword, [FromQuery] int? roleId)
        {
            try
            {
                var q = _context.Users.AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    string kw = keyword.Trim().ToLower();
                    q = q.Where(u => u.Username.ToLower().Contains(kw) ||
                                     u.FullName.ToLower().Contains(kw) ||
                                     (u.PhoneNumber != null && u.PhoneNumber.Contains(kw)) ||
                                     (u.Email != null && u.Email.ToLower().Contains(kw)));
                }

                if (roleId.HasValue && roleId.Value > 0)
                {
                    q = q.Where(u => u.RoleId == roleId.Value);
                }

                var list = await q.OrderByDescending(u => u.UserId)
                    .Select(u => new UserItemDto
                    {
                        UserId = u.UserId,
                        Username = u.Username,
                        FullName = u.FullName,
                        PhoneNumber = u.PhoneNumber,
                        Email = u.Email,
                        RoleId = u.RoleId,
                        RoleName = u.RoleId == 1 ? "Admin" : "Customer"
                    })
                    .ToListAsync();

                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi truy vấn danh sách người dùng: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            try
            {
                var u = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == id);
                if (u == null) return NotFound($"Không tìm thấy tài khoản #{id}");

                return Ok(new UserItemDto
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    FullName = u.FullName,
                    PhoneNumber = u.PhoneNumber,
                    Email = u.Email,
                    RoleId = u.RoleId,
                    RoleName = u.RoleId == 1 ? "Admin" : "Customer"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi: {ex.Message}");
            }
        }

        [HttpPost("create-admin")]
        public async Task<IActionResult> CreateUserByAdmin([FromBody] AdminCreateUserDto req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            {
                return BadRequest("Tên đăng nhập và mật khẩu không được để trống.");
            }

            string username = req.Username.Trim();
            bool exists = await _context.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower());
            if (exists)
            {
                return BadRequest($"Tên đăng nhập '{username}' đã tồn tại!");
            }

            var newUser = new User
            {
                Username = username,
                PasswordHash = ComputeSha256Hash(req.Password),
                FullName = string.IsNullOrWhiteSpace(req.FullName) ? username : req.FullName.Trim(),
                Email = req.Email?.Trim(),
                PhoneNumber = req.PhoneNumber?.Trim(),
                RoleId = req.RoleId > 0 ? req.RoleId : 2
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Tạo tài khoản thành công!", UserId = newUser.UserId });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UserUpdateDto dto)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null) return NotFound($"Không tìm thấy tài khoản #{id}");

                if (!string.IsNullOrWhiteSpace(dto.Username) && dto.Username.Trim() != user.Username)
                {
                    string targetUser = dto.Username.Trim();
                    bool isExisted = await _context.Users.AnyAsync(u => u.UserId != id && u.Username.ToLower() == targetUser.ToLower());
                    if (isExisted)
                    {
                        return BadRequest($"Tên đăng nhập '{targetUser}' đã được tài khoản khác sử dụng!");
                    }
                    user.Username = targetUser;
                }

                user.FullName = string.IsNullOrWhiteSpace(dto.FullName) ? user.FullName : dto.FullName.Trim();
                user.PhoneNumber = dto.PhoneNumber?.Trim();
                user.Email = dto.Email?.Trim();

                if (dto.RoleId.HasValue && dto.RoleId.Value > 0)
                {
                    user.RoleId = dto.RoleId.Value;
                }

                await _context.SaveChangesAsync();
                return Ok(new { Message = $"Đã cập nhật thông tin tài khoản #{id} thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi cập nhật tài khoản: {ex.Message}");
            }
        }

        [HttpPut("{id}/reset-password")]
        public async Task<IActionResult> AdminResetPassword(int id, [FromBody] AdminResetPasswordDto req)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(req.NewPassword))
                {
                    return BadRequest("Mật khẩu mới không được để trống.");
                }

                var user = await _context.Users.FindAsync(id);
                if (user == null) return NotFound($"Không tìm thấy tài khoản #{id}");

                user.PasswordHash = ComputeSha256Hash(req.NewPassword);
                await _context.SaveChangesAsync();

                return Ok(new { Message = $"Đã đặt lại mật khẩu cho tài khoản #{id} thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi reset mật khẩu: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null) return NotFound($"Không tìm thấy tài khoản #{id}");

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                return Ok(new { Message = $"Đã xóa tài khoản #{id} thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi xóa tài khoản: {ex.Message}");
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            {
                return BadRequest("Vui lòng điền đầy đủ tài khoản và mật khẩu.");
            }

            string inputUser = req.Username.Trim().ToLower();
            string hashedPass = ComputeSha256Hash(req.Password);

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username.ToLower() == inputUser ||
                                          (u.Email != null && u.Email.ToLower() == inputUser) ||
                                          (u.PhoneNumber != null && u.PhoneNumber == inputUser));

            if (user == null)
            {
                return NotFound("Tài khoản chưa tồn tại trong hệ thống.");
            }

            bool isPasswordCorrect = user.PasswordHash == hashedPass || user.PasswordHash == req.Password;
            if (!isPasswordCorrect)
            {
                return BadRequest("Mật khẩu không chính xác.");
            }

            return Ok(new
            {
                CustomerId = user.UserId,
                UserId = user.UserId,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber ?? user.Username,
                Email = user.Email ?? "",
                Role = user.RoleId == 1 ? "Admin" : "Customer"
            });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Password))
            {
                return BadRequest("Dữ liệu đăng ký không hợp lệ.");
            }

            string username = !string.IsNullOrWhiteSpace(req.Username)
                ? req.Username.Trim()
                : (!string.IsNullOrWhiteSpace(req.PhoneNumber) ? req.PhoneNumber.Trim() : req.Email?.Trim() ?? "");

            if (string.IsNullOrWhiteSpace(username))
            {
                return BadRequest("Vui lòng nhập tên đăng nhập, số điện thoại hoặc email.");
            }

            bool exists = await _context.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower() ||
                                                            (!string.IsNullOrEmpty(req.Email) && u.Email != null && u.Email.ToLower() == req.Email.ToLower()) ||
                                                            (!string.IsNullOrEmpty(req.PhoneNumber) && u.PhoneNumber != null && u.PhoneNumber == req.PhoneNumber));

            if (exists)
            {
                return BadRequest("Tài khoản, Số điện thoại hoặc Email này đã được đăng ký!");
            }

            var newUser = new User
            {
                Username = username,
                PasswordHash = ComputeSha256Hash(req.Password),
                FullName = string.IsNullOrWhiteSpace(req.FullName) ? username : req.FullName.Trim(),
                Email = req.Email?.Trim(),
                PhoneNumber = req.PhoneNumber?.Trim(),
                RoleId = 2
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Đăng ký tài khoản thành công!", UserId = newUser.UserId });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.NewPassword))
            {
                return BadRequest("Thông tin yêu cầu không hợp lệ.");
            }

            string inputUser = req.Username.Trim().ToLower();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == inputUser ||
                                                                     (u.Email != null && u.Email.ToLower() == inputUser) ||
                                                                     (u.PhoneNumber != null && u.PhoneNumber == inputUser));

            if (user == null)
            {
                return NotFound("Không tìm thấy tài khoản tương ứng.");
            }

            user.PasswordHash = ComputeSha256Hash(req.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Đổi mật khẩu thành công!" });
        }
    }

    public class UserItemDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; } = "Customer";
    }

    public class UserUpdateDto
    {
        public string? Username { get; set; }
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public int? RoleId { get; set; }
    }

    public class AdminCreateUserDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public int RoleId { get; set; } = 2;
    }

    public class AdminResetPasswordDto
    {
        public string NewPassword { get; set; } = string.Empty;
    }

    public class LoginRequestDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterRequestDto
    {
        public string? Username { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string Password { get; set; } = string.Empty;
    }

    public class ForgotPasswordDto
    {
        public string Username { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}