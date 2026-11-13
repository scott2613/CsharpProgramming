using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;
using ComputerStore.API.Models;
using ComputerStore.API.Security;
using ComputerStore.Application.DTOs.Accounts;
using ComputerStore.Application.Exceptions;
using ComputerStore.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json;

namespace ComputerStore.API.Controllers;

[Authorize]
[AutoValidateAntiforgeryToken]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AccountController(IAccountService accounts, IOrderService orders, ICartService cart,
    IDataProtectionProvider protection, PasswordResetMail resetMail) : Controller
{
    public const string UserSessionKey = "DTB_CustomerUser";
    private int UserId => AccountIdentity.Id(User);
    public static ProfileViewModel ToProfile(AccountDto a) => new()
    {
        CustomerId = a.UserId,
        Username = a.Username,
        FullName = a.FullName,
        Email = a.Email ?? "",
        PhoneNumber = a.PhoneNumber ?? "",
        Address = a.Address
    };
    private void SyncProfile(AccountDto a) => HttpContext.Session.SetString(UserSessionKey, JsonSerializer.Serialize(ToProfile(a)));
    private string? LocalReturnUrl(string? value) => !string.IsNullOrEmpty(value) && Url.IsLocalUrl(value) ? value : null;

    [HttpGet, AllowAnonymous]
    public IActionResult Login(string? returnUrl)
    { ViewBag.ReturnUrl = LocalReturnUrl(returnUrl); return View(new LoginViewModel()); }

    [HttpPost, AllowAnonymous, EnableRateLimiting("account-login")]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl, CancellationToken ct)
    {
        ViewBag.ReturnUrl = LocalReturnUrl(returnUrl);
        if (!ModelState.IsValid) return View(model);
        try
        {
            var login = await accounts.AuthenticateAsync(new AccountLoginDto
            {
                Username = model.Username.Trim(),
                Password = model.Password,
                RememberMe = model.RememberMe
            }, ct);
            if (login is null) { ModelState.AddModelError("", "Tài khoản hoặc mật khẩu không chính xác."); return View(model); }
            await AccountIdentity.SignInAsync(HttpContext, login, model.RememberMe); SyncProfile(login.Account);
            TempData["SuccessMessage"] = $"Chào mừng {login.Account.FullName}.";
            return LocalReturnUrl(returnUrl) is string local ? LocalRedirect(local) : RedirectToAction("Index", "Product");
        }
        catch (AccountOperationException ex) { ModelState.AddModelError("", ex.Message); return View(model); }
    }
    [HttpGet, AllowAnonymous]
    public IActionResult Register() => View(new RegisterViewModel());
    [HttpPost, AllowAnonymous, EnableRateLimiting("account-login")]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        try
        {
            await accounts.RegisterAsync(new AccountRegisterDto
            {
                Username = model.PhoneNumber.Trim(),
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                Email = model.Email,
                Address = model.Address,
                Password = model.Password
            }, ct);
            TempData["SuccessMessage"] = "Đăng ký thành công. Vui lòng đăng nhập."; return RedirectToAction(nameof(Login));
        }
        catch (AccountOperationException ex) { ModelState.AddModelError("", ex.Message); return View(model); }
    }

    [HttpGet]
    public async Task<IActionResult> Profile(CancellationToken ct)
    { var a = await accounts.ProfileAsync(UserId, ct); SyncProfile(a); return View(ToProfile(a)); }
    [HttpPost]
    public async Task<IActionResult> Profile(ProfileViewModel model, CancellationToken ct)
    {
        // Luôn lấy ID/tên đăng nhập từ danh tính, không tin hidden field gửi lên.
        var current = await accounts.ProfileAsync(UserId, ct);
        model.CustomerId = current.UserId; model.Username = current.Username;
        ModelState.Remove(nameof(model.CustomerId)); ModelState.Remove(nameof(model.Username));
        if (!ModelState.IsValid) return View(model);
        try
        {
            var saved = await accounts.UpdateProfileAsync(UserId, new AccountProfileUpdateDto
            {
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                Email = model.Email,
                Address = model.Address
            }, ct);
            SyncProfile(saved); TempData["SuccessMessage"] = "Đã lưu hồ sơ vào cơ sở dữ liệu."; return RedirectToAction(nameof(Profile));
        }
        catch (AccountOperationException ex) { ModelState.AddModelError("", ex.Message); return View(model); }
    }
    [HttpGet]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());
    [HttpPost]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        try
        {
            await accounts.ChangePasswordAsync(UserId, new AccountPasswordChangeDto { OldPassword = model.OldPassword, NewPassword = model.NewPassword }, ct);
            await AccountIdentity.SignOutAsync(HttpContext);
            TempData["SuccessMessage"] = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại."; return RedirectToAction(nameof(Login));
        }
        catch (AccountOperationException ex) { ModelState.AddModelError("", ex.Message); return View(model); }
    }
    [HttpGet]
    public async Task<IActionResult> Orders(CancellationToken ct)
    {
        ViewBag.Orders = await accounts.OwnedOrdersAsync(UserId, ct);
        return View(ToProfile(await accounts.ProfileAsync(UserId, ct)));
    }
    [HttpPost]
    public async Task<IActionResult> CancelOrder(int id, CancellationToken ct)
    {
        await orders.CancelOwnedAsync(UserId, id, ct);
        return Ok(new { message = "Đã hủy đơn và hoàn kho." });
    }
    [HttpPost]
    public async Task<IActionResult> Rebuy(int id, CancellationToken ct)
    {
        try
        {
            await cart.RebuyAsync(UserId, id, ct);
            return RedirectToAction("Index", "Cart");
        }
        catch (OrderOperationException ex)
        {
            TempData["RebuyError"] = ex.Message;
            return RedirectToAction(nameof(Orders));
        }
    }

    [HttpGet]
    public IActionResult Logout() => View();
    [HttpPost, ActionName("Logout")]
    public async Task<IActionResult> ConfirmLogout()
    { await AccountIdentity.SignOutAsync(HttpContext); TempData["SuccessMessage"] = "Đã đăng xuất."; return RedirectToAction(nameof(Login)); }

    [HttpGet, AllowAnonymous]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    private ITimeLimitedDataProtector PasswordChangeProtector =>
        protection.CreateProtector("ComputerStore.CustomerPasswordChange.v1").ToTimeLimitedDataProtector();

    [HttpPost, AllowAnonymous, EnableRateLimiting("account-login")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        var userId = await accounts.FindCustomerIdAsync(model.Identifier.Trim(), ct);
        if (userId is null)
        {
            ModelState.AddModelError(nameof(model.Identifier),
                "Tài khoản không tồn tại hoặc không có vai trò Khách hàng (Customer). Vui lòng kiểm tra tài khoản trong Quản lý tài khoản.");
            return View(model);
        }

        ViewBag.PasswordChangeTicket = PasswordChangeProtector.Protect(
            JsonSerializer.Serialize(userId.Value), TimeSpan.FromMinutes(10));
        return View(nameof(ChangePassword), new ChangePasswordViewModel());
    }

    [HttpPost, AllowAnonymous, EnableRateLimiting("account-login")]
    public async Task<IActionResult> ChangePasswordWithAccount(ChangePasswordViewModel model, string? ticket, CancellationToken ct)
    {
        int? userId = null;
        if (!string.IsNullOrWhiteSpace(ticket) && ticket.Length <= 1024)
        {
            try { userId = JsonSerializer.Deserialize<int>(PasswordChangeProtector.Unprotect(ticket)); }
            catch (Exception ex) when (ex is CryptographicException or JsonException or FormatException) { }
        }

        if (userId is null)
        {
            TempData["PasswordChangeNotice"] = "Phiên đổi mật khẩu đã hết hiệu lực. Vui lòng nhập lại tài khoản.";
            return RedirectToAction(nameof(ForgotPassword));
        }

        ViewBag.PasswordChangeTicket = ticket;
        if (!ModelState.IsValid) return View(nameof(ChangePassword), model);
        try
        {
            await accounts.ChangeCustomerPasswordAsync(userId.Value, new AccountPasswordChangeDto
            {
                OldPassword = model.OldPassword,
                NewPassword = model.NewPassword
            }, ct);
            if (User.Identity?.IsAuthenticated == true && UserId == userId.Value)
                await AccountIdentity.SignOutAsync(HttpContext);
            TempData["SuccessMessage"] = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại.";
            return RedirectToAction(nameof(Login));
        }
        catch (AccountOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(nameof(ChangePassword), model);
        }
    }

    [HttpGet, AllowAnonymous]
    public IActionResult ForgotPasswordEmail()
    {
        ViewBag.MailEnabled = resetMail.Origin is not null;
        ViewBag.UsesLocalInbox = resetMail.UsesLocalInbox;
        return View(new ForgotPasswordEmailViewModel());
    }

    private ITimeLimitedDataProtector ResetProtector =>
        protection.CreateProtector("ComputerStore.CustomerPasswordReset.v1").ToTimeLimitedDataProtector();

    private sealed record ResetTicket(int UserId, string Email, string Stamp);

    [HttpPost, AllowAnonymous, EnableRateLimiting("account-login")]
    public async Task<IActionResult> ForgotPasswordRequest(ForgotPasswordEmailViewModel model, CancellationToken ct)
    {
        var origin = resetMail.Origin;
        ViewBag.MailEnabled = origin is not null;
        ViewBag.UsesLocalInbox = resetMail.UsesLocalInbox;
        if (origin is null)
        {
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return View(nameof(ForgotPasswordEmail), model);
        }
        if (!ModelState.IsValid) return View(nameof(ForgotPasswordEmail), model);
        var account = await accounts.PasswordResetTargetAsync(model.Email.Trim(), ct);
        if (account?.Account.Email is string email)
        {
            var token = ResetProtector.Protect(JsonSerializer.Serialize(
                new ResetTicket(account.Account.UserId, email, account.Stamp)), TimeSpan.FromMinutes(20));
            var path = Url.Action(nameof(ResetPassword), "Account", new { token });
            if (path is not null) await resetMail.SendAsync(email, new Uri(origin, path), ct);
        }
        TempData["ResetNotice"] = "Nếu email thuộc tài khoản hợp lệ, bạn sẽ nhận được liên kết đặt lại mật khẩu.";
        return RedirectToAction(nameof(ForgotPasswordEmail));
    }

    private ResetTicket? ReadResetTicket(string? token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 4096) return null;
        try { return JsonSerializer.Deserialize<ResetTicket>(ResetProtector.Unprotect(token)); }
        catch (Exception ex) when (ex is CryptographicException or JsonException or FormatException) { return null; }
    }

    [HttpGet, AllowAnonymous]
    public IActionResult ResetPassword(string? token)
    {
        Response.Headers["Referrer-Policy"] = "no-referrer";
        var valid = !string.IsNullOrEmpty(ReadResetTicket(token)?.Email);
        if (!valid) ModelState.AddModelError("", "Liên kết không hợp lệ hoặc đã hết hạn.");
        return View(new ResetPasswordViewModel { Token = valid ? token! : "" });
    }

    [HttpPost, AllowAnonymous, EnableRateLimiting("account-login")]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model, CancellationToken ct)
    {
        Response.Headers["Referrer-Policy"] = "no-referrer";
        if (!ModelState.IsValid) return View(model);
        var ticket = ReadResetTicket(model.Token);
        if (string.IsNullOrEmpty(ticket?.Email))
        {
            ModelState.AddModelError("", "Liên kết không hợp lệ hoặc đã hết hạn.");
            return View(model);
        }
        try
        {
            await accounts.ResetByEmailAsync(ticket.UserId, ticket.Email, ticket.Stamp, model.NewPassword, ct);
            if (User.Identity?.IsAuthenticated == true && UserId == ticket.UserId)
                await AccountIdentity.SignOutAsync(HttpContext);
            TempData["SuccessMessage"] = "Đã đặt lại mật khẩu. Vui lòng đăng nhập.";
            return RedirectToAction(nameof(Login));
        }
        catch (AccountOperationException ex) { ModelState.AddModelError("", ex.Message); return View(model); }
    }
    [HttpGet, AllowAnonymous]
    public IActionResult AccessDenied() { Response.StatusCode = 403; return View(); }
}
