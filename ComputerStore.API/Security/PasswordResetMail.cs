using System.Net;
using System.Net.Mail;

namespace ComputerStore.API.Security;

public sealed class PasswordResetMail(
    IConfiguration config, IWebHostEnvironment environment, ILogger<PasswordResetMail> logger)
{
    public bool UsesLocalInbox =>
        environment.IsDevelopment() &&
        config.GetValue<bool>("PasswordResetLocalInbox") &&
        !config.GetSection("PasswordReset").Exists();

    public Uri? Origin
    {
        get
        {
            if (UsesLocalInbox)
                return new Uri("https://localhost:7275");

            if (string.IsNullOrWhiteSpace(config["PasswordReset:Host"]) ||
                !MailAddress.TryCreate(config["PasswordReset:From"], out _) ||
                !int.TryParse(config["PasswordReset:Port"], out var port) || port is < 1 or > 65535 ||
                !Uri.TryCreate(config["PasswordReset:PublicOrigin"], UriKind.Absolute, out var origin) ||
                origin.Scheme != Uri.UriSchemeHttps || origin.UserInfo.Length != 0 ||
                origin.AbsolutePath != "/" || origin.Query.Length != 0 || origin.Fragment.Length != 0)
                return null;
            return origin;
        }
    }

    public async Task SendAsync(string recipient, Uri resetLink, CancellationToken ct)
    {
        var localInbox = UsesLocalInbox;
        var sender = localInbox ? "reset@dtbstore.test" : config["PasswordReset:From"]!;
        var host = localInbox ? "127.0.0.1" : config["PasswordReset:Host"]!;
        var port = localInbox ? 1025 : int.Parse(config["PasswordReset:Port"]!);
        using var message = new MailMessage(sender, recipient)
        {
            Subject = "DTBStore - Đặt lại mật khẩu",
            Body = $"Mở liên kết sau trong vòng 20 phút để đặt lại mật khẩu:\n{resetLink}\nNếu bạn không yêu cầu, hãy bỏ qua email."
        };
        using var client = new SmtpClient(host, port)
        {
            EnableSsl = !localInbox,
            UseDefaultCredentials = false
        };
        if (!localInbox && !string.IsNullOrWhiteSpace(config["PasswordReset:Username"]))
            client.Credentials = new NetworkCredential(config["PasswordReset:Username"], config["PasswordReset:Password"]);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        try { await client.SendMailAsync(message, timeout.Token); }
        catch (SmtpException ex) { logger.LogWarning("Gửi email đặt lại mật khẩu thất bại ({Status}).", ex.StatusCode); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        { logger.LogWarning("Gửi email đặt lại mật khẩu quá thời gian chờ."); }
    }
}
