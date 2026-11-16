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

// Yêu cầu đăng nhập mặc định. Các trang đăng nhập và khôi phục dùng AllowAnonymous để cho phép khách truy cập.
[Authorize]
// Kiểm tra mã chống CSRF cho các yêu cầu thay đổi dữ liệu.
[AutoValidateAntiforgeryToken]
// Yêu cầu trình duyệt không lưu phản hồi chứa thông tin tài khoản.
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AccountController(IAccountService accounts, IOrderService orders, ICartService cart,
    IDataProtectionProvider protection, PasswordResetMail resetMail) : Controller
{
    // Khóa session dùng để lưu thông tin hồ sơ hiển thị trên giao diện.
    public const string UserSessionKey = "DTB_CustomerUser";
    // Lấy mã người dùng từ danh tính đã xác thực của yêu cầu hiện tại.
    private int UserId => AccountIdentity.Id(User);
    // Chuyển DTO tài khoản thành dữ liệu mà View cần hiển thị.
    public static ProfileViewModel ToProfile(AccountDto a) => new()
    {
        CustomerId = a.UserId,
        Username = a.Username,
        FullName = a.FullName,
        Email = a.Email ?? "",
        PhoneNumber = a.PhoneNumber ?? "",
        Address = a.Address
    };
    // Đồng bộ bản hồ sơ trong session sau khi đăng nhập hoặc cập nhật.
    private void SyncProfile(AccountDto a) => HttpContext.Session.SetString(UserSessionKey, JsonSerializer.Serialize(ToProfile(a)));
    // Chỉ chấp nhận đường dẫn nội bộ để tránh chuyển hướng người dùng sang trang ngoài.
    private string? LocalReturnUrl(string? value) => !string.IsNullOrEmpty(value) && Url.IsLocalUrl(value) ? value : null;

    [HttpGet, AllowAnonymous]
    // Mở biểu mẫu đăng nhập và giữ lại trang nội bộ cần quay về.
    public IActionResult Login(string? returnUrl)
    { ViewBag.ReturnUrl = LocalReturnUrl(returnUrl); return View(new LoginViewModel()); }

    [HttpPost, AllowAnonymous, EnableRateLimiting("account-login")]
    // Xử lý dữ liệu đăng nhập. Chính sách account-login giới hạn tần suất gửi yêu cầu.
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl, CancellationToken ct)
    {
        ViewBag.ReturnUrl = LocalReturnUrl(returnUrl);
        if (!ModelState.IsValid) return View(model);
        try
        {
            // Giao việc xác thực thông tin đăng nhập cho tầng Application.
            var login = await accounts.AuthenticateAsync(new AccountLoginDto
            {
                Username = model.Username.Trim(),
                Password = model.Password,
                RememberMe = model.RememberMe
            }, ct);
            // Hiển thị chung một lỗi khi thông tin đăng nhập không hợp lệ.
            if (login is null) { ModelState.AddModelError("", "Tài khoản hoặc mật khẩu không chính xác."); return View(model); }
            // Thiết lập phiên đăng nhập và đồng bộ hồ sơ hiển thị trong session.
            await AccountIdentity.SignInAsync(HttpContext, login, model.RememberMe); SyncProfile(login.Account);
            TempData["SuccessMessage"] = $"Chào mừng {login.Account.FullName}.";
            return LocalReturnUrl(returnUrl) is string local ? LocalRedirect(local) : RedirectToAction("Index", "Product");
        }
        catch (AccountOperationException ex) { ModelState.AddModelError("", ex.Message); return View(model); }
    }
    [HttpGet, AllowAnonymous]
    public IActionResult Register() => View(new RegisterViewModel());
    [HttpPost, AllowAnonymous, EnableRateLimiting("account-login")]
    // Kiểm tra biểu mẫu rồi yêu cầu service tạo tài khoản.
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        try
        {
            await accounts.RegisterAsync(new AccountRegisterDto
            {
                // Dùng số điện thoại đã loại bỏ khoảng trắng ngoài làm tên đăng nhập.
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
        // Bỏ dữ liệu binding cũ của hai trường định danh để View dùng giá trị đã lấy từ máy chủ.
        ModelState.Remove(nameof(model.CustomerId)); ModelState.Remove(nameof(model.Username));
        if (!ModelState.IsValid) return View(model);
        try
        {
            // Lưu các trường hồ sơ được phép chỉnh sửa cho tài khoản hiện tại.
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
            // Chuyển mật khẩu cũ và mới cho service kiểm tra và cập nhật.
            await accounts.ChangePasswordAsync(UserId, new AccountPasswordChangeDto { OldPassword = model.OldPassword, NewPassword = model.NewPassword }, ct);
            await AccountIdentity.SignOutAsync(HttpContext);
            TempData["SuccessMessage"] = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại."; return RedirectToAction(nameof(Login));
        }
        catch (AccountOperationException ex) { ModelState.AddModelError("", ex.Message); return View(model); }
    }
    [HttpGet]
    public async Task<IActionResult> Orders(CancellationToken ct)
    {
        // Nạp lịch sử đơn hàng thuộc tài khoản đang đăng nhập.
        ViewBag.Orders = await accounts.OwnedOrdersAsync(UserId, ct);
        return View(ToProfile(await accounts.ProfileAsync(UserId, ct)));
    }
    [HttpPost]
    public async Task<IActionResult> CancelOrder(int id, CancellationToken ct)
    {
        // Giao việc kiểm tra quyền sở hữu và hủy đơn cho tầng nghiệp vụ.
        await orders.CancelOwnedAsync(UserId, id, ct);
        return Ok(new { message = "Đã hủy đơn và hoàn kho." });
    }
    [HttpPost]
    public async Task<IActionResult> Rebuy(int id, CancellationToken ct)
    {
        try
        {
            // Thêm lại sản phẩm của đơn cũ vào giỏ thông qua service.
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
    // Giữ tên tuyến Logout cho POST xác nhận đăng xuất.
    [HttpPost, ActionName("Logout")]
    public async Task<IActionResult> ConfirmLogout()
    { await AccountIdentity.SignOutAsync(HttpContext); TempData["SuccessMessage"] = "Đã đăng xuất."; return RedirectToAction(nameof(Login)); }

    [HttpGet, AllowAnonymous]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    // Dùng mục đích bảo vệ riêng cho vé đổi mật khẩu theo tài khoản.
    private ITimeLimitedDataProtector PasswordChangeProtector =>
        protection.CreateProtector("ComputerStore.CustomerPasswordChange.v1").ToTimeLimitedDataProtector();

    [HttpPost, AllowAnonymous, EnableRateLimiting("account-login")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        // Tra cứu tài khoản khách hàng trước khi hiển thị biểu mẫu đổi mật khẩu.
        var userId = await accounts.FindCustomerIdAsync(model.Identifier.Trim(), ct);
        // Nếu không đọc được vé hợp lệ, yêu cầu người dùng xác định lại tài khoản.
        if (userId is null)
        {
            ModelState.AddModelError(nameof(model.Identifier),
                "Tài khoản không tồn tại hoặc không có vai trò Khách hàng (Customer). Vui lòng kiểm tra tài khoản trong Quản lý tài khoản.");
            return View(model);
        }

        // Tạo vé đổi mật khẩu có thời hạn 10 phút và gắn vé với mã khách hàng.
        ViewBag.PasswordChangeTicket = PasswordChangeProtector.Protect(
            JsonSerializer.Serialize(userId.Value), TimeSpan.FromMinutes(10));
        return View(nameof(ChangePassword), new ChangePasswordViewModel());
    }

    [HttpPost, AllowAnonymous, EnableRateLimiting("account-login")]
    public async Task<IActionResult> ChangePasswordWithAccount(ChangePasswordViewModel model, string? ticket, CancellationToken ct)
    {
        int? userId = null;
        // Giới hạn độ dài đầu vào trước khi giải mã và kiểm tra thời hạn của vé.
        if (!string.IsNullOrWhiteSpace(ticket) && ticket.Length <= 1024)
        {
            try { userId = JsonSerializer.Deserialize<int>(PasswordChangeProtector.Unprotect(ticket)); }
            catch (Exception ex) when (ex is CryptographicException or JsonException or FormatException) { }
        }

        // Nếu không đọc được vé hợp lệ, yêu cầu người dùng xác định lại tài khoản.
        if (userId is null)
        {
            TempData["PasswordChangeNotice"] = "Phiên đổi mật khẩu đã hết hiệu lực. Vui lòng nhập lại tài khoản.";
            return RedirectToAction(nameof(ForgotPassword));
        }

        ViewBag.PasswordChangeTicket = ticket;
        if (!ModelState.IsValid) return View(nameof(ChangePassword), model);
        try
        {
            // Đổi mật khẩu cho tài khoản trong vé sau khi service xác minh mật khẩu cũ.
            await accounts.ChangeCustomerPasswordAsync(userId.Value, new AccountPasswordChangeDto
            {
                OldPassword = model.OldPassword,
                NewPassword = model.NewPassword
            }, ct);
            // Đăng xuất nếu phiên hiện tại thuộc chính tài khoản vừa đổi mật khẩu.
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

    // Dùng mục đích bảo vệ riêng cho token đặt lại mật khẩu qua email.
    private ITimeLimitedDataProtector ResetProtector =>
        protection.CreateProtector("ComputerStore.CustomerPasswordReset.v1").ToTimeLimitedDataProtector();

    // Token đặt lại mang mã người dùng, email và dấu xác thực của tài khoản.
    private sealed record ResetTicket(int UserId, string Email, string Stamp);

    [HttpPost, AllowAnonymous, EnableRateLimiting("account-login")]
    public async Task<IActionResult> ForgotPasswordRequest(ForgotPasswordEmailViewModel model, CancellationToken ct)
    {
        // Dùng địa chỉ gốc đã cấu hình để tạo liên kết gửi qua email.
        var origin = resetMail.Origin;
        ViewBag.MailEnabled = origin is not null;
        ViewBag.UsesLocalInbox = resetMail.UsesLocalInbox;
        if (origin is null)
        {
            // Báo tính năng gửi email chưa sẵn sàng nếu chưa có địa chỉ gốc.
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return View(nameof(ForgotPasswordEmail), model);
        }
        if (!ModelState.IsValid) return View(nameof(ForgotPasswordEmail), model);
        var account = await accounts.PasswordResetTargetAsync(model.Email.Trim(), ct);
        if (account?.Account.Email is string email)
        {
            // Mã hóa dữ liệu của yêu cầu đặt lại mật khẩu với thời hạn 20 phút.
            var token = ResetProtector.Protect(JsonSerializer.Serialize(
                new ResetTicket(account.Account.UserId, email, account.Stamp)), TimeSpan.FromMinutes(20));
            var path = Url.Action(nameof(ResetPassword), "Account", new { token });
            if (path is not null) await resetMail.SendAsync(email, new Uri(origin, path), ct);
        }
        // Dùng cùng một thông báo dù có tìm được địa chỉ email hay không.
        TempData["ResetNotice"] = "Nếu email thuộc tài khoản hợp lệ, bạn sẽ nhận được liên kết đặt lại mật khẩu.";
        return RedirectToAction(nameof(ForgotPasswordEmail));
    }

    // Kiểm tra kích thước, thời hạn và cấu trúc token trước khi sử dụng dữ liệu.
    private ResetTicket? ReadResetTicket(string? token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 4096) return null;
        try { return JsonSerializer.Deserialize<ResetTicket>(ResetProtector.Unprotect(token)); }
        catch (Exception ex) when (ex is CryptographicException or JsonException or FormatException) { return null; }
    }

    [HttpGet, AllowAnonymous]
    public IActionResult ResetPassword(string? token)
    {
        // Yêu cầu trình duyệt không gửi URL chứa token qua tiêu đề Referer.
        Response.Headers["Referrer-Policy"] = "no-referrer";
        var valid = !string.IsNullOrEmpty(ReadResetTicket(token)?.Email);
        if (!valid) ModelState.AddModelError("", "Liên kết không hợp lệ hoặc đã hết hạn.");
        return View(new ResetPasswordViewModel { Token = valid ? token! : "" });
    }

    [HttpPost, AllowAnonymous, EnableRateLimiting("account-login")]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model, CancellationToken ct)
    {
        // Yêu cầu trình duyệt không gửi URL chứa token qua tiêu đề Referer.
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
            // Chuyển thông tin trong token cho service xác minh trước khi ghi mật khẩu mới.
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
