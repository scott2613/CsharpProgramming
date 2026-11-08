using ComputerStore.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ComputerStore.Admin.Services
{
    public class ApiService
    {

        public static string BaseUrl { get; } = NormalizeBaseUrl();
        private static readonly System.Net.CookieContainer Cookies = new();
        private static readonly HttpClient SharedClient = CreateHttpClient();
        private readonly HttpClient _client = SharedClient;
        public ApiService() { }
        public static UserDto? CurrentUser { get; private set; }
        public string? LastError { get; private set; }
        private static string NormalizeBaseUrl()
        {
            var value = Environment.GetEnvironmentVariable("COMPUTERSTORE_API_BASEURL") ?? "https://localhost:7275/api/";
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https") || (!string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)))
                throw new InvalidOperationException("COMPUTERSTORE_API_BASEURL không hợp lệ.");
            return uri.AbsoluteUri.TrimEnd('/') + "/";
        }
        public static string? ResolveImageUrl(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            value = value.Trim();
            if (value.StartsWith("//") || value.Contains((char)92)) return null;
            if (value.StartsWith("~/")) value = value[1..];
            if (value.StartsWith("/")) return new Uri(new Uri(BaseUrl), value).AbsoluteUri;
            if (Uri.TryCreate(value, UriKind.Absolute, out var external))
            {
                if (external.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(external.UserInfo))
                    return null;
                if (external.Scheme == "https" && external.IsDefaultPort &&
                    external.Host.Equals("img.tgdd.vn", StringComparison.OrdinalIgnoreCase))
                {
                    const string marker = "/https://cdn.tgdd.vn/";
                    int index = external.AbsolutePath.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                    if (index >= 0 && Uri.TryCreate(external.AbsolutePath[(index + 1)..],
                            UriKind.Absolute, out var original) &&
                        original.Host.Equals("cdn.tgdd.vn", StringComparison.OrdinalIgnoreCase) &&
                        string.IsNullOrEmpty(original.UserInfo) && original.IsDefaultPort &&
                        System.IO.Path.GetExtension(original.AbsolutePath).ToLowerInvariant()
                            is ".png" or ".jpg" or ".jpeg")
                        return original.AbsoluteUri;
                }
                return external.AbsoluteUri;
            }
            if (value.StartsWith("~/")) value = value[1..];
            return new Uri(new Uri(BaseUrl), "/" + value.TrimStart('/')).AbsoluteUri;
        }
        public static HttpClient CreateHttpClient() => new(new AccountHttpHandler(Cookies))
        { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromSeconds(30) };
        public async Task<bool> LoginAsync(string username, string password)
        {
            LastError = null;
            try
            {
                using var response = await _client.PostAsJsonAsync("users/login", new { Username = username.Trim(), Password = password, AdminClient = true });
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    try { using var json = JsonDocument.Parse(body); LastError = json.RootElement.TryGetProperty("message", out var m) ? m.GetString() : "Đăng nhập thất bại. Kiểm tra tài khoản/mật khẩu."; }
                    catch (JsonException) { LastError = "Đăng nhập thất bại (HTTP " + (int)response.StatusCode + ")."; }
                    return false;
                }
                var user = await response.Content.ReadFromJsonAsync<UserDto>();
                if (user is null || user.RoleName != "Admin") { LastError = "Chỉ Admin được dùng ứng dụng quản trị."; return false; }
                CurrentUser = user; return true;
            }
            catch (HttpRequestException) { LastError = "Không kết nối được API. Kiểm tra URL, API đang chạy và chứng chỉ HTTPS đã được tin cậy."; return false; }
            catch (TaskCanceledException) { LastError = "API phản hồi quá thời gian chờ."; return false; }
            catch (JsonException) { LastError = "Phản hồi API không đúng JSON. Kiểm tra URL API và phiên bản backend."; return false; }
        }
        public async Task LogoutAsync()
        {
            try
            {
                using var response = await _client.PostAsJsonAsync("users/logout", new { });
                if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized) response.EnsureSuccessStatusCode();
            }
            finally
            {
                CurrentUser = null;
                // Xóa cookie local kể cả khi API tạm ngắt. Phiên server không có API revoke
                // riêng; cookie trên tiến trình này được loại bỏ trước khi đăng nhập lại.
                foreach (System.Net.Cookie cookie in Cookies.GetAllCookies()) cookie.Expired = true;
            }
        }

        // ==================== QUẢN LÝ DANH MỤC & SẢN PHẨM ====================
        public async Task<List<Category>> GetCategoriesAsync()
        {
            var value = await ReadOrderJsonAsync<List<Category>>("categories", CancellationToken.None);
            ShowLegacyOrderError(); return value ?? new();
        }
        public async Task<List<Product>> GetProductsAsync()
        {
            var value = await ReadOrderJsonAsync<List<Product>>("products", CancellationToken.None);
            ShowLegacyOrderError(); return value ?? new();
        }
        public async Task<bool> AddProductAsync(Product product)
        {
            var ok = await WriteOrderJsonAsync(HttpMethod.Post, "products", product, CancellationToken.None);
            ShowLegacyOrderError(); return ok;
        }
        public async Task<bool> UpdateProductAsync(Product product, int expectedStockQuantity)
        {
            var ok = await WriteOrderJsonAsync(HttpMethod.Put, $"products/{product.ProductId}", new {
                product.ProductId, product.Name, product.Price, product.StockQuantity, product.Specifications,
                product.ImageUrl, product.CategoryId, ExpectedStockQuantity = expectedStockQuantity }, CancellationToken.None);
            ShowLegacyOrderError(); return ok;
        }
        public async Task<bool> DeleteProductAsync(int id)
        {
            var ok = await WriteOrderJsonAsync(HttpMethod.Delete, $"products/{id}", null, CancellationToken.None);
            ShowLegacyOrderError(); return ok;
        }

        // ==================== QUẢN LÝ ĐƠN HÀNG (ORDERS) ====================
        // Các overload có CancellationToken không mở MessageBox; form tự hiển thị
        // một lần và phân biệt lỗi tải với danh sách thực sự trống.
        public int? LastOrderStatusCode { get; private set; }
        private static readonly JsonSerializerOptions OrderJson = new() { PropertyNameCaseInsensitive = true };
        private void ResetOrderError() { LastError = null; LastOrderStatusCode = null; }

        private async Task SetOrderHttpErrorAsync(HttpResponseMessage response, CancellationToken ct)
        {
            LastOrderStatusCode = (int)response.StatusCode;
            string? detail = null;
            if (LastOrderStatusCode < 500)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                try
                {
                    using var doc = JsonDocument.Parse(body);
                    if (doc.RootElement.ValueKind == JsonValueKind.Object)
                    {
                        if (doc.RootElement.TryGetProperty("message", out var msg) && msg.ValueKind == JsonValueKind.String)
                            detail = msg.GetString();
                        else if (doc.RootElement.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                        {
                            var messages = errors.EnumerateObject()
                                .Where(p => p.Value.ValueKind == JsonValueKind.Array)
                                .SelectMany(p => p.Value.EnumerateArray())
                                .Where(v => v.ValueKind == JsonValueKind.String)
                                .Select(v => v.GetString()).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct();
                            detail = string.Join("\n", messages);
                        }
                    }
                }
                catch (JsonException) { } // Không hiển thị nguyên HTML/stack trace.
            }
            LastError = LastOrderStatusCode switch
            {
                401 => "Phiên đăng nhập Admin đã hết hạn. Hãy đóng ứng dụng và đăng nhập lại.",
                403 => "Tài khoản không có quyền thực hiện chức năng quản lý đơn hàng.",
                404 => "Dữ liệu không còn tồn tại hoặc không thể truy cập. Hãy tải lại danh sách.",
                400 => detail ?? "Yêu cầu không hợp lệ hoặc mã CSRF không hợp lệ. Hãy tải lại; nếu còn lỗi, đăng nhập lại.",
                409 => detail ?? "Đơn hàng đã thay đổi hoặc thao tác không phù hợp trạng thái hiện tại. Hãy tải lại.",
                429 => "Có quá nhiều yêu cầu. Vui lòng chờ rồi thử lại.",
                >= 500 => "API gặp lỗi xử lý. Hãy kiểm tra log API và tải lại đơn hàng trước khi thao tác tiếp.",
                _ => detail ?? $"API trả về HTTP {LastOrderStatusCode}. Kiểm tra địa chỉ API và phiên đăng nhập."
            };
            if (LastError.Length > 700) LastError = LastError[..700] + "…";
            if (LastOrderStatusCode == 401) CurrentUser = null;
        }
        private void SetOrderNetworkError(bool writing, bool timedOut)
        {
            LastError = timedOut ? "API phản hồi quá thời gian chờ." :
                "Không kết nối được API. Kiểm tra API đang chạy, địa chỉ và chứng chỉ HTTPS.";
            if (writing) LastError += " Chưa xác nhận được kết quả thao tác; hãy tải lại danh sách để đối chiếu trước khi thực hiện lại.";
        }
        private async Task<T?> ReadOrderJsonAsync<T>(string path, CancellationToken ct) where T : class
        {
            ResetOrderError();
            try
            {
                using var response = await _client.GetAsync(path, ct);
                if (!response.IsSuccessStatusCode) { await SetOrderHttpErrorAsync(response, ct); return null; }
                var json = await response.Content.ReadAsStringAsync(ct);
                return JsonSerializer.Deserialize<T>(json, OrderJson) ?? throw new JsonException("Phản hồi rỗng.");
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (OperationCanceledException) { SetOrderNetworkError(false, true); return null; }
            catch (HttpRequestException) { SetOrderNetworkError(false, false); return null; }
            catch (JsonException) { LastError = "Dữ liệu từ API không đúng định dạng. Kiểm tra phiên bản backend."; return null; }
        }
        private async Task<bool> WriteOrderJsonAsync(HttpMethod method, string path, object? payload, CancellationToken ct)
        {
            ResetOrderError();
            try
            {
                using var request = new HttpRequestMessage(method, path);
                if (payload != null) request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                // Dùng nguyên SharedClient/AccountHttpHandler hiện có: cookie và CSRF.
                using var response = await _client.SendAsync(request, ct);
                if (!response.IsSuccessStatusCode) { await SetOrderHttpErrorAsync(response, ct); return false; }
                return true;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (OperationCanceledException) { SetOrderNetworkError(true, true); return false; }
            catch (HttpRequestException) { SetOrderNetworkError(true, false); return false; }
            catch (JsonException) { LastError = "Không nhận được mã xác minh hợp lệ từ API. Hãy đăng nhập lại."; return false; }
        }
        private void ShowLegacyOrderError()
        {
            if (LastError != null) MessageBox.Show(LastError, "Thông báo API", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        // Giữ nguyên chữ ký cũ để các form khác không cần chỉnh.
        public async Task<List<OrderDto>> GetOrdersAsync()
        {
            var result = await GetOrdersAsync(CancellationToken.None);
            ShowLegacyOrderError(); return result;
        }
        public async Task<List<OrderDto>> GetOrdersAsync(CancellationToken ct) =>
            await ReadOrderJsonAsync<List<OrderDto>>("orders", ct) ?? new List<OrderDto>();

        public Task<OrderDto?> GetOrderAsync(int orderId, CancellationToken ct = default) =>
            ReadOrderJsonAsync<OrderDto>($"orders/{orderId}", ct);

        public async Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus)
        {
            var result = await UpdateOrderStatusAsync(orderId, newStatus, CancellationToken.None);
            ShowLegacyOrderError(); return result;
        }
        public Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus, CancellationToken ct) =>
            WriteOrderJsonAsync(HttpMethod.Put, $"orders/{orderId}/status", new { Status = newStatus }, ct);

        public async Task<bool> UpdateOrderShippingAsync(int orderId, string customerName, string phone, string address)
        {
            var result = await UpdateOrderShippingAsync(orderId, customerName, phone, address, CancellationToken.None);
            ShowLegacyOrderError(); return result;
        }
        public Task<bool> UpdateOrderShippingAsync(int orderId, string customerName, string phone, string address, CancellationToken ct) =>
            WriteOrderJsonAsync(HttpMethod.Put, $"orders/{orderId}/shipping",
                new { CustomerName = customerName, CustomerPhone = phone, DeliveryAddress = address }, ct);

        public async Task<bool> DeleteOrderAsync(int orderId)
        {
            var result = await DeleteOrderAsync(orderId, CancellationToken.None);
            ShowLegacyOrderError(); return result;
        }
        public Task<bool> DeleteOrderAsync(int orderId, CancellationToken ct) =>
            WriteOrderJsonAsync(HttpMethod.Delete, $"orders/{orderId}", null, ct);

        // ==================== QUẢN LÝ TÀI KHOẢN (USERS) ====================
        private static string? OptionalText(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        public async Task<List<UserDto>> GetUsersAsync(string? keyword = null, int? roleId = null)
        {
            var value = await ReadOrderJsonAsync<List<UserDto>>($"users?keyword={Uri.EscapeDataString(keyword ?? "")}&roleId={roleId}", CancellationToken.None);
            ShowLegacyOrderError(); return value ?? new();
        }
        public async Task<bool> CreateUserAsync(string username, string password, string fullName, string? phone, string? email, int roleId)
        {
            var ok = await WriteOrderJsonAsync(HttpMethod.Post, "users/create-admin", new {
                Username=username.Trim(), Password=password, FullName=fullName.Trim(), PhoneNumber=OptionalText(phone), Email=OptionalText(email), RoleId=roleId }, CancellationToken.None);
            ShowLegacyOrderError(); return ok;
        }
        public async Task<bool> UpdateUserAsync(int userId, string username, string fullName, string? phone, string? email, int roleId)
        {
            var ok = await WriteOrderJsonAsync(HttpMethod.Put, $"users/{userId}", new {
                Username=username.Trim(), FullName=fullName.Trim(), PhoneNumber=OptionalText(phone), Email=OptionalText(email), RoleId=roleId }, CancellationToken.None);
            ShowLegacyOrderError(); return ok;
        }
        public async Task<bool> ResetUserPasswordAsync(int userId, string newPassword)
        {
            var ok = await WriteOrderJsonAsync(HttpMethod.Put, $"users/{userId}/reset-password", new { NewPassword=newPassword }, CancellationToken.None);
            ShowLegacyOrderError(); return ok;
        }
        public async Task<bool> DeleteUserAsync(int userId)
        {
            var ok = await WriteOrderJsonAsync(HttpMethod.Delete, $"users/{userId}", null, CancellationToken.None);
            ShowLegacyOrderError(); return ok;
        }
    }
}
