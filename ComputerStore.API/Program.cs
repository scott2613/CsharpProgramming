using ComputerStore.API.Controllers;
using ComputerStore.API.Security;
using ComputerStore.Application.Interfaces;
using ComputerStore.Application.Services;
using ComputerStore.Infrastructure.Data;
using ComputerStore.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using ComputerStore.Application.DTOs.Accounts;
using ComputerStore.Core.Entities;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net;

namespace ComputerStore.API;
public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString)) throw new InvalidOperationException("Chưa cấu hình ConnectionStrings:DefaultConnection.");
        builder.Services.AddDbContext<ApplicationDbContext>(o => o.UseSqlServer(connectionString));
        builder.Services.AddScoped<IProductReadRepository, ProductReadRepository>();
        builder.Services.AddScoped<IProductQueryService, ProductQueryService>();
        builder.Services.AddScoped<IProductWriteRepository, ProductWriteRepository>();
        builder.Services.AddScoped<IProductCommandService, ProductCommandService>();
        builder.Services.AddScoped<IOrderStore, OrderStore>();
        builder.Services.AddScoped<IOrderService, OrderService>();
        builder.Services.AddScoped<ICartService, CartService>();
        builder.Services.AddScoped<IAccountRepository, AccountRepository>();
        builder.Services.AddScoped<IAccountService, AccountService>();
        builder.Services.AddScoped<PasswordResetMail>();
        builder.Services.Configure<LegacyPasswordOptions>(builder.Configuration.GetSection("Authentication"));
        builder.Services.AddSingleton<IAccountPasswordService, AccountPasswordService>();
        builder.Services.AddScoped<AccountCookieEvents>();
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o => {
            o.Cookie.Name = AccountIdentity.CookieName; o.Cookie.HttpOnly = true; o.Cookie.SameSite = SameSiteMode.Lax;
            o.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
            o.LoginPath = "/Account/Login"; o.AccessDeniedPath = "/Account/AccessDenied";
            o.ExpireTimeSpan = TimeSpan.FromMinutes(60); o.SlidingExpiration = false; o.EventsType = typeof(AccountCookieEvents);
        });
        builder.Services.AddAuthorization();
        builder.Services.Configure<ForwardedHeadersOptions>(o =>
        {
            o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            foreach (var value in builder.Configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? Array.Empty<string>())
            {
                if (!IPAddress.TryParse(value, out var proxy)) throw new InvalidOperationException("ReverseProxy:KnownProxies phải là địa chỉ IP hợp lệ.");
                o.KnownProxies.Add(proxy);
            }
        });
        builder.Services.AddAntiforgery(o => {
            o.HeaderName = "X-CSRF-TOKEN"; o.Cookie.Name = "ComputerStore.Csrf"; o.Cookie.HttpOnly = true;
            o.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        });
        builder.Services.AddControllersWithViews(o => o.Conventions.Add(new ApiAccountConvention()))
            .AddJsonOptions(o => o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
        builder.Services.AddOpenApi();
        builder.Services.AddRateLimiter(o => {
            o.RejectionStatusCode = 429;
            o.AddPolicy("account-login", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }));
        });

        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddSession(o => {
            o.IdleTimeout = TimeSpan.FromMinutes(60); o.Cookie.Name = "ComputerStore.Session";
            o.Cookie.HttpOnly = true; o.Cookie.IsEssential = true; o.Cookie.SameSite = SameSiteMode.Lax;
            o.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        });
        builder.Services.AddHttpContextAccessor();
        var app = builder.Build();
        if (args.Contains("--create-admin"))
        {
            await CreateInitialAdminAsync(app.Services);
            return;
        }
        app.UseForwardedHeaders();
        if (!app.Environment.IsDevelopment()) { app.UseHsts(); app.UseHttpsRedirection(); }
        app.UseMiddleware<AccountErrorMiddleware>();
        app.UseStaticFiles(); app.UseRouting(); app.UseSession();
        app.UseAuthentication(); app.UseAuthorization(); app.UseRateLimiter();
        // Session chỉ phục vụ giao diện cũ. Cookie đã xác thực mới là danh tính.
        app.Use(async (context, next) => {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var accounts = context.RequestServices.GetRequiredService<IAccountService>();
                var a = context.Items[AccountIdentity.ValidatedAccountItem] as AccountDto
                    ?? await accounts.ProfileAsync(AccountIdentity.Id(context.User), context.RequestAborted);
                context.Session.SetString(AccountController.UserSessionKey, JsonSerializer.Serialize(AccountController.ToProfile(a)));
            }
            else if (context.Session.GetString(AccountController.UserSessionKey) is not null) context.Session.Clear();
            await next(context);
        });
        if (app.Environment.IsDevelopment()) app.MapOpenApi();
        app.MapStaticAssets(); app.MapControllers();
        app.MapControllerRoute("default", "{controller=Product}/{action=Index}/{id?}").WithStaticAssets();
        await app.RunAsync();
    }
    // Chỉ chạy khi người vận hành gọi --create-admin; không có mật khẩu mặc định.
    private static async Task CreateInitialAdminAsync(IServiceProvider services)
    {
        string username = Environment.GetEnvironmentVariable("BootstrapAdmin__Username")?.Trim() ?? "";
        string password = Environment.GetEnvironmentVariable("BootstrapAdmin__Password") ?? "";
        if (username.Length is < 1 or > 50 || string.IsNullOrWhiteSpace(password) || password.Length is < 8 or > 128)
            throw new InvalidOperationException("Đặt BootstrapAdmin__Username (1..50) và BootstrapAdmin__Password (8..128) trong biến môi trường.");
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var passwords = scope.ServiceProvider.GetRequiredService<IAccountPasswordService>();
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        // Không tự nâng quyền tài khoản có sẵn, kể cả khi trùng email/số điện thoại.
        if (await db.Users.AnyAsync(u => u.Username == username || u.Email == username || u.PhoneNumber == username))
            throw new InvalidOperationException("Định danh đã tồn tại; không tự thay mật khẩu hoặc nâng quyền.");
        var role = await db.Roles.SingleOrDefaultAsync(r => r.RoleId == 1 && r.RoleName == StoreRoles.Admin)
            ?? throw new InvalidOperationException("Chạy SQL nâng cấp schema/quyền trước khi tạo Admin.");
        var user = new User { Username = username, FullName = "Quản trị viên", RoleId = role.RoleId, Address = "" };
        user.PasswordHash = passwords.Hash(user, password);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        Console.WriteLine("Đã tạo Admin. Không ghi mật khẩu ra log.");
    }

}
