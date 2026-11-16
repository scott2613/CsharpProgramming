using ComputerStore.Application.DTOs.Accounts;
using ComputerStore.Application.Interfaces;
using ComputerStore.API.Security;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ComputerStore.API.Controllers;

[ApiController]
// Tiền tố chung của API tài khoản.
[Route("api/users")]
// Yêu cầu đăng nhập mặc định. Các API như đăng nhập và lấy token CSRF có AllowAnonymous.
[Authorize]
// Xác minh token chống CSRF cho các yêu cầu thay đổi dữ liệu.
[AutoValidateAntiforgeryToken]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class UsersController(IAccountService accounts) : ControllerBase
{
    // Lấy mã người gọi từ danh tính đã xác thực.
    private int ActorId => AccountIdentity.Id(User);

    [HttpGet("csrf"), AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Csrf([FromServices] IAntiforgery antiforgery)
    {
        // Tạo token để client gửi qua tiêu đề X-CSRF-TOKEN khi gọi các API thay đổi dữ liệu.
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { token = tokens.RequestToken, headerName = "X-CSRF-TOKEN" });
    }

    [HttpPost("login"), AllowAnonymous, EnableRateLimiting("account-login")]
    public async Task<IActionResult> Login(AccountLoginDto dto, CancellationToken ct)
    {
        // Nhờ service xác thực thông tin đăng nhập.
        var login = await accounts.AuthenticateAsync(dto, ct);
        if (login is null) return Unauthorized(new { message = "Tài khoản hoặc mật khẩu không chính xác." });
        // Thiết lập phiên đăng nhập và áp dụng lựa chọn ghi nhớ đăng nhập.
        await AccountIdentity.SignInAsync(HttpContext, login, dto.RememberMe);
        return Ok(login.Account);
    }
    [HttpPost("register"), AllowAnonymous, EnableRateLimiting("account-login")]
    public async Task<IActionResult> Register(AccountRegisterDto dto, CancellationToken ct)
    {
        // Tạo tài khoản rồi trả mã người dùng mới cùng HTTP 201.
        var account = await accounts.RegisterAsync(dto, ct);
        return StatusCode(201, new { message = "Đăng ký thành công. Vui lòng đăng nhập.", userId = account.UserId });
    }
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    { await AccountIdentity.SignOutAsync(HttpContext); return Ok(new { message = "Đã đăng xuất." }); }

    // Lấy hồ sơ của tài khoản đang đăng nhập.
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct) => Ok(await accounts.ProfileAsync(ActorId, ct));
    // Chỉ cập nhật hồ sơ của tài khoản đang đăng nhập.
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe(AccountProfileUpdateDto dto, CancellationToken ct) => Ok(await accounts.UpdateProfileAsync(ActorId, dto, ct));
    // Lấy các đơn hàng thuộc tài khoản đang đăng nhập.
    [HttpGet("me/orders")]
    public async Task<IActionResult> MyOrders(CancellationToken ct) => Ok(await accounts.OwnedOrdersAsync(ActorId, ct));

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(AccountPasswordChangeDto dto, CancellationToken ct)
    {
        // Không nhận UserId/Username từ client để chọn tài khoản cần đổi.
        await accounts.ChangePasswordAsync(ActorId, dto, ct);
        // Kết thúc phiên hiện tại sau khi mật khẩu đổi thành công.
        await AccountIdentity.SignOutAsync(HttpContext);
        return Ok(new { message = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại." });
    }

    // Chỉ Admin được lấy danh sách tài khoản và lọc theo từ khóa hoặc vai trò.
    [HttpGet, Authorize(Roles = StoreRoles.Admin)]
    public async Task<IActionResult> GetUsers([FromQuery] string? keyword, [FromQuery] int? roleId, CancellationToken ct) =>
        Ok(await accounts.ListAsync(ActorId, keyword, roleId, ct));
    [HttpGet("{id:int:min(1)}")]
    public async Task<IActionResult> GetUserById(int id, CancellationToken ct)
    {
        // Cho phép người dùng xem hồ sơ của chính họ. Xem hồ sơ người khác cần vai trò Admin.
        if (id == ActorId) return Ok(await accounts.ProfileAsync(ActorId, ct));
        if (!User.IsInRole(StoreRoles.Admin)) return Forbid();
        return Ok(await accounts.GetByAdminAsync(ActorId, id, ct));
    }
    // Tuyến quản trị dùng để tạo tài khoản theo dữ liệu Admin cung cấp.
    [HttpPost("create-admin"), Authorize(Roles = StoreRoles.Admin)]
    public async Task<IActionResult> CreateUserByAdmin(AdminCreateAccountDto dto, CancellationToken ct)
    {
        var account = await accounts.CreateByAdminAsync(ActorId, dto, ct);
        return CreatedAtAction(nameof(GetUserById), new { id = account.UserId }, account);
    }
    // Chỉ Admin được cập nhật tài khoản theo mã chỉ định.
    [HttpPut("{id:int:min(1)}"), Authorize(Roles = StoreRoles.Admin)]
    public async Task<IActionResult> UpdateUser(int id, AdminUpdateAccountDto dto, CancellationToken ct)
    { await accounts.UpdateByAdminAsync(ActorId, id, dto, ct); return Ok(new { message = "Đã cập nhật tài khoản." }); }
    // Cho phép Admin đặt lại mật khẩu cho tài khoản theo mã.
    [HttpPut("{id:int:min(1)}/reset-password"), Authorize(Roles = StoreRoles.Admin)]
    public async Task<IActionResult> AdminResetPassword(int id, AdminPasswordResetDto dto, CancellationToken ct)
    { await accounts.ResetByAdminAsync(ActorId, id, dto.NewPassword, ct); return Ok(new { message = "Đã đặt lại mật khẩu. Người dùng cần đăng nhập lại." }); }
    // Chỉ Admin được gửi yêu cầu xóa tài khoản theo mã.
    [HttpDelete("{id:int:min(1)}"), Authorize(Roles = StoreRoles.Admin)]
    public async Task<IActionResult> DeleteUser(int id, CancellationToken ct)
    { await accounts.DeleteByAdminAsync(ActorId, id, ct); return Ok(new { message = "Đã xóa tài khoản." }); }

    [HttpPost("forgot-password"), AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        // Trả HTTP 303 để client mở trang quên mật khẩu bằng GET.
        Response.Headers["Location"] = Url.Action("ForgotPassword", "Account") ?? "/Account/ForgotPassword";
        return StatusCode(303);
    }
}
