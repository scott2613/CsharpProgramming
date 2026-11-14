using System.Security.Claims;
using ComputerStore.Application.DTOs.Accounts;
using ComputerStore.Application.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ComputerStore.API.Security;

public static class AccountIdentity
{
    public const string CookieName = "ComputerStore.Auth";
    public const string ValidatedAccountItem = "ComputerStore.ValidatedAccount";
    public const string StampClaim = "computerstore:stamp";
    public static int Id(ClaimsPrincipal user) => int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) && id > 0 ? id : 0;
    public static async Task SignInAsync(HttpContext context, AuthenticatedAccount account, bool remember)
    {
        context.Session.Clear();
        var a = account.Account;
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]{
            new Claim(ClaimTypes.NameIdentifier,a.UserId.ToString()),new Claim(ClaimTypes.Name,a.Username),
            new Claim(ClaimTypes.Role,a.RoleName),new Claim(StampClaim,account.Stamp)
        }, CookieAuthenticationDefaults.AuthenticationScheme));
        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
        {
            IsPersistent = remember,
            ExpiresUtc = DateTimeOffset.UtcNow.Add(remember ? TimeSpan.FromDays(14) : TimeSpan.FromMinutes(60))
        });
        context.User = principal;
    }
    public static async Task SignOutAsync(HttpContext context)
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        context.Session.Clear();
        context.User = new ClaimsPrincipal(new ClaimsIdentity());
    }
}

public sealed class AccountCookieEvents(IAccountService accounts) : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var user = await accounts.SessionAsync(AccountIdentity.Id(context.Principal!), context.HttpContext.RequestAborted);
        var stamp = context.Principal?.FindFirstValue(AccountIdentity.StampClaim);
        if (user is null || stamp != user.Stamp || context.Principal?.FindFirstValue(ClaimTypes.Role) != user.Account.RoleName)
        {
            context.RejectPrincipal();
            context.HttpContext.Session.Clear();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
        else context.HttpContext.Items[AccountIdentity.ValidatedAccountItem] = user.Account;
    }
    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    {
        if (context.Request.Path.StartsWithSegments("/api")) context.Response.StatusCode = 401;
        else context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    }
    public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context)
    {
        if (context.Request.Path.StartsWithSegments("/api")) context.Response.StatusCode = 403;
        else context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    }
}
