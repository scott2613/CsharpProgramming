using System.Net;
using System.Net.Http;
using System.Text.Json;
namespace ComputerStore.Admin.Services;

// Các ApiService và form dùng chung cookie đăng nhập trong MỘT tiến trình Admin.
internal sealed class AccountHttpHandler(CookieContainer cookies) : DelegatingHandler(
    new HttpClientHandler { CookieContainer = cookies, UseCookies = true, AllowAutoRedirect = false })
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (request.Method != HttpMethod.Get && request.Method != HttpMethod.Head && request.Method != HttpMethod.Options)
        {
            using var csrfRequest = new HttpRequestMessage(HttpMethod.Get, new Uri(new Uri(ApiService.BaseUrl), "users/csrf"));
            using var response = await base.SendAsync(csrfRequest, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
            var token = json.RootElement.GetProperty("token").GetString();
            if (string.IsNullOrWhiteSpace(token)) throw new HttpRequestException("Không nhận được mã xác minh yêu cầu từ API.");
            request.Headers.Remove("X-CSRF-TOKEN"); request.Headers.Add("X-CSRF-TOKEN", token);
        }
        return await base.SendAsync(request, ct).ConfigureAwait(false);
    }
}
