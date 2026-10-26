using ComputerStore.Core.Entities;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ComputerStore.Admin.Services
{
    public class UserDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; } = "Customer";
    }

    public class ApiService
    {
        private readonly HttpClient _client;
        public const string BaseUrl = "https://localhost:7275/api/";

        public ApiService()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };
            _client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
        }

        // ==================== QUẢN LÝ DANH MỤC & SẢN PHẨM ====================

        public async Task<List<Category>> GetCategoriesAsync()
        {
            try
            {
                var response = await _client.GetAsync("categories");
                if (!response.IsSuccessStatusCode) return new List<Category>();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Category>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<Category>();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối khi lấy danh mục: {ex.Message}", "Lỗi API", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return new List<Category>();
            }
        }

        public async Task<List<Product>> GetProductsAsync()
        {
            try
            {
                var response = await _client.GetAsync("products");
                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Không lấy được danh sách sản phẩm ({response.StatusCode}): {err}", "Lỗi API", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return new List<Product>();
                }

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Product>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<Product>();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối khi lấy sản phẩm: {ex.Message}", "Lỗi API", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return new List<Product>();
            }
        }

        public async Task<bool> AddProductAsync(Product product)
        {
            try
            {
                var json = JsonSerializer.Serialize(product);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _client.PostAsync("products", content);

                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Lỗi từ Server ({response.StatusCode}): {err}", "Lỗi API khi thêm", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối khi thêm: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            try
            {
                var response = await _client.DeleteAsync($"products/{id}");
                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Lỗi xóa sản phẩm ({response.StatusCode}): {err}", "Lỗi API", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối khi xóa: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // ==================== QUẢN LÝ ĐƠN HÀNG (ORDERS) ====================

        public async Task<List<OrderDto>> GetOrdersAsync()
        {
            try
            {
                var response = await _client.GetAsync("orders");
                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Không lấy được danh sách đơn hàng (HTTP {(int)response.StatusCode} - {response.StatusCode}): {err}", "Lỗi API", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return new List<OrderDto>();
                }

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<OrderDto>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<OrderDto>();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối khi lấy đơn hàng: {ex.Message}", "Lỗi API", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return new List<OrderDto>();
            }
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus)
        {
            try
            {
                var updatePayload = new { Status = newStatus };
                var json = JsonSerializer.Serialize(updatePayload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _client.PutAsync($"orders/{orderId}/status", content);
                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Lỗi cập nhật trạng thái (HTTP {(int)response.StatusCode} - {response.StatusCode}): {err}", "Lỗi API", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối khi cập nhật đơn hàng: {ex.Message}", "Lỗi API", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public async Task<bool> UpdateOrderShippingAsync(int orderId, string customerName, string phone, string address)
        {
            try
            {
                var payload = new
                {
                    CustomerName = customerName,
                    CustomerPhone = phone,
                    DeliveryAddress = address
                };

                var response = await _client.PutAsJsonAsync($"orders/{orderId}/shipping", payload);
                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Lỗi cập nhật thông tin nhận hàng ({response.StatusCode}): {err}", "Lỗi API", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối khi cập nhật thông tin: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public async Task<bool> DeleteOrderAsync(int orderId)
        {
            try
            {
                var response = await _client.DeleteAsync($"orders/{orderId}");
                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Không thể xóa đơn hàng ({response.StatusCode}): {err}", "Lỗi API", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối khi xóa đơn hàng: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // ==================== QUẢN LÝ TÀI KHOẢN (USERS) ====================

        public async Task<List<UserDto>> GetUsersAsync(string? keyword = null, int? roleId = null)
        {
            try
            {
                var encodedKeyword = Uri.EscapeDataString(keyword ?? "");
                var url = $"users?keyword={encodedKeyword}&roleId={roleId}";

                var response = await _client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Không tải được danh sách tài khoản ({response.StatusCode}): {err}", "Lỗi API", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return new List<UserDto>();
                }

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<UserDto>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<UserDto>();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối khi lấy danh sách tài khoản: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return new List<UserDto>();
            }
        }

        public async Task<bool> CreateUserAsync(string username, string password, string fullName, string? phone, string? email, int roleId)
        {
            try
            {
                var payload = new
                {
                    Username = username,
                    Password = password,
                    FullName = fullName,
                    PhoneNumber = phone,
                    Email = email,
                    RoleId = roleId
                };

                var response = await _client.PostAsJsonAsync("users/create-admin", payload);
                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Không thể tạo tài khoản ({response.StatusCode}): {err}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public async Task<bool> UpdateUserAsync(int userId, string username, string fullName, string? phone, string? email, int roleId)
        {
            try
            {
                var payload = new
                {
                    Username = username,
                    FullName = fullName,
                    PhoneNumber = phone,
                    Email = email,
                    RoleId = roleId
                };

                var response = await _client.PutAsJsonAsync($"users/{userId}", payload);
                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Lỗi cập nhật người dùng ({response.StatusCode}): {err}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public async Task<bool> ResetUserPasswordAsync(int userId, string newPassword)
        {
            try
            {
                var payload = new { NewPassword = newPassword };
                var response = await _client.PutAsJsonAsync($"users/{userId}/reset-password", payload);
                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Lỗi đặt lại mật khẩu ({response.StatusCode}): {err}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            try
            {
                var response = await _client.DeleteAsync($"users/{userId}");
                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Không thể xóa tài khoản ({response.StatusCode}): {err}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
    }
}