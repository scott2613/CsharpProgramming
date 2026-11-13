using ComputerStore.Application.DTOs.Accounts;
using ComputerStore.Application.Interfaces;
using ComputerStore.API.Security;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ComputerStore.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
[AutoValidateAntiforgeryToken]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class UsersController(IAccountService accounts) : ControllerBase
{
    private int ActorId => AccountIdentity.Id(User);

    [HttpGet("csrf"), AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Csrf([FromServices] IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { token = tokens.RequestToken, headerName = "X-CSRF-TOKEN" });
    }

    [HttpPost("login"), AllowAnonymous, EnableRateLimiting("account-login")]
    public async Task<IActionResult> Login(AccountLoginDto dto, CancellationToken ct)
    {
        var login = await accounts.AuthenticateAsync(dto, ct);
        if (login is null) return Unauthorized(new { message = "Tài khoản hoặc mật khẩu không chính xác." });
        await AccountIdentity.SignInAsync(HttpContext, login, dto.RememberMe);
        return Ok(login.Account);
    }
    [HttpPost("register"), AllowAnonymous, EnableRateLimiting("account-login")]
    public async Task<IActionResult> Register(AccountRegisterDto dto, CancellationToken ct)
    {
        var account = await accounts.RegisterAsync(dto, ct);
        return StatusCode(201, new { message = "Đăng ký thành công. Vui lòng đăng nhập.", userId = account.UserId });
    }
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    { await AccountIdentity.SignOutAsync(HttpContext); return Ok(new { message = "Đã đăng xuất." }); }

    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct) => Ok(await accounts.ProfileAsync(ActorId, ct));
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe(AccountProfileUpdateDto dto, CancellationToken ct) => Ok(await accounts.UpdateProfileAsync(ActorId, dto, ct));
    [HttpGet("me/orders")]
    public async Task<IActionResult> MyOrders(CancellationToken ct) => Ok(await accounts.OwnedOrdersAsync(ActorId, ct));

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(AccountPasswordChangeDto dto, CancellationToken ct)
    {
        // Không nhận UserId/Username từ client để chọn tài khoản cần đổi.
        await accounts.ChangePasswordAsync(ActorId, dto, ct);
        await AccountIdentity.SignOutAsync(HttpContext);
        return Ok(new { message = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại." });
    }

    [HttpGet, Authorize(Roles = StoreRoles.Admin)]
    public async Task<IActionResult> GetUsers([FromQuery] string? keyword, [FromQuery] int? roleId, CancellationToken ct) =>
        Ok(await accounts.ListAsync(ActorId, keyword, roleId, ct));
    [HttpGet("{id:int:min(1)}")]
    public async Task<IActionResult> GetUserById(int id, CancellationToken ct)
    {
        if (id == ActorId) return Ok(await accounts.ProfileAsync(ActorId, ct));
        if (!User.IsInRole(StoreRoles.Admin)) return Forbid();
        return Ok(await accounts.GetByAdminAsync(ActorId, id, ct));
    }
    [HttpPost("create-admin"), Authorize(Roles = StoreRoles.Admin)]
    public async Task<IActionResult> CreateUserByAdmin(AdminCreateAccountDto dto, CancellationToken ct)
    {
        var account = await accounts.CreateByAdminAsync(ActorId, dto, ct);
        return CreatedAtAction(nameof(GetUserById), new { id = account.UserId }, account);
    }
    [HttpPut("{id:int:min(1)}"), Authorize(Roles = StoreRoles.Admin)]
    public async Task<IActionResult> UpdateUser(int id, AdminUpdateAccountDto dto, CancellationToken ct)
    { await accounts.UpdateByAdminAsync(ActorId, id, dto, ct); return Ok(new { message = "Đã cập nhật tài khoản." }); }
    [HttpPut("{id:int:min(1)}/reset-password"), Authorize(Roles = StoreRoles.Admin)]
    public async Task<IActionResult> AdminResetPassword(int id, AdminPasswordResetDto dto, CancellationToken ct)
    { await accounts.ResetByAdminAsync(ActorId, id, dto.NewPassword, ct); return Ok(new { message = "Đã đặt lại mật khẩu. Người dùng cần đăng nhập lại." }); }
    [HttpDelete("{id:int:min(1)}"), Authorize(Roles = StoreRoles.Admin)]
    public async Task<IActionResult> DeleteUser(int id, CancellationToken ct)
    { await accounts.DeleteByAdminAsync(ActorId, id, ct); return Ok(new { message = "Đã xóa tài khoản." }); }

    [HttpPost("forgot-password"), AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        Response.Headers["Location"] = Url.Action("ForgotPassword", "Account") ?? "/Account/ForgotPassword";
        return StatusCode(303);
    }
}
