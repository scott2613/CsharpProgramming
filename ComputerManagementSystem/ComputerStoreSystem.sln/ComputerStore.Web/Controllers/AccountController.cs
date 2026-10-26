using ComputerStore.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace ComputerStore.Web.Controllers
{
    // DTO tiếp nhận dữ liệu lịch sử đơn hàng từ API
    public class CustomerOrderHistoryDto
    {
        public int OrderId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string DeliveryAddress { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Chờ xử lý";
        public List<CustomerOrderDetailDto> OrderDetails { get; set; } = new();
    }

    public class CustomerOrderDetailDto
    {
        public int OrderDetailId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class AccountController : Controller
    {
        public const string UserSessionKey = "DTB_CustomerUser";
        private readonly IHttpClientFactory _httpClientFactory;

        public AccountController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            if (!ModelState.IsValid) return View(model);

            try
            {
                var client = _httpClientFactory.CreateClient("StoreAPI");

                var loginPayload = new
                {
                    Username = model.Username.Trim(),
                    Password = model.Password
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(loginPayload),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await client.PostAsync("users/login", content);

                if (!response.IsSuccessStatusCode)
                {
                    ModelState.AddModelError("", "Tài khoản chưa tồn tại hoặc mật khẩu không chính xác. Vui lòng đăng ký nếu chưa có tài khoản!");
                    return View(model);
                }

                var json = await response.Content.ReadAsStringAsync();
                var loggedCustomer = JsonSerializer.Deserialize<ProfileViewModel>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (loggedCustomer == null)
                {
                    ModelState.AddModelError("", "Không thể phân giải thông tin tài khoản. Vui lòng thử lại!");
                    return View(model);
                }

                HttpContext.Session.SetString(UserSessionKey, JsonSerializer.Serialize(loggedCustomer));
                TempData["SuccessMessage"] = $"Đăng nhập thành công! Chào mừng {loggedCustomer.FullName}.";

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Product");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Lỗi kết nối máy chủ xác thực: {ex.Message}");
                return View(model);
            }
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                var client = _httpClientFactory.CreateClient("StoreAPI");
                var payload = new
                {
                    Username = !string.IsNullOrWhiteSpace(model.PhoneNumber) ? model.PhoneNumber.Trim() : model.Email.Trim(),
                    FullName = model.FullName.Trim(),
                    PhoneNumber = model.PhoneNumber?.Trim() ?? "",
                    Email = model.Email?.Trim() ?? "",
                    Address = model.Address?.Trim() ?? "",
                    Password = model.Password
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await client.PostAsync("users/register", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorMsg = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError("", string.IsNullOrWhiteSpace(errorMsg) ? "Số điện thoại/Email này đã được đăng ký tài khoản!" : errorMsg);
                    return View(model);
                }

                TempData["SuccessMessage"] = "Đăng ký tài khoản thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Lỗi khi đăng ký tài khoản: {ex.Message}");
                return View(model);
            }
        }

        // GET: /Account/Profile - HIỂN THỊ HỒ SƠ CÁ NHÂN
        [HttpGet]
        public IActionResult Profile()
        {
            var userJson = HttpContext.Session.GetString(UserSessionKey);
            if (string.IsNullOrEmpty(userJson))
            {
                return RedirectToAction("Login", new { returnUrl = "/Account/Profile" });
            }

            var profile = JsonSerializer.Deserialize<ProfileViewModel>(userJson) ?? new ProfileViewModel();
            return View(profile);
        }

        // POST: /Account/Profile
        [HttpPost]
        public IActionResult Profile(ProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            HttpContext.Session.SetString(UserSessionKey, JsonSerializer.Serialize(model));
            TempData["SuccessMessage"] = "Cập nhật thông tin cá nhân thành công!";
            return View(model);
        }

        // GET: /Account/Orders - TRANG QUẢN LÝ THÔNG TIN ĐƠN HÀNG RIÊNG BIỆT
        [HttpGet]
        public async Task<IActionResult> Orders()
        {
            var userJson = HttpContext.Session.GetString(UserSessionKey);
            if (string.IsNullOrEmpty(userJson))
            {
                return RedirectToAction("Login", new { returnUrl = "/Account/Orders" });
            }

            var profile = JsonSerializer.Deserialize<ProfileViewModel>(userJson) ?? new ProfileViewModel();
            ViewBag.Orders = await FetchCustomerOrdersAsync(profile);

            return View(profile);
        }

        // GET: /Account/Logout
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Remove(UserSessionKey);
            TempData["SuccessMessage"] = "Đã đăng xuất tài khoản.";
            return RedirectToAction("Login");
        }

        // GET: /Account/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Account/ForgotPassword
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                var client = _httpClientFactory.CreateClient("StoreAPI");
                var payload = new
                {
                    Username = model.Username.Trim(),
                    NewPassword = model.NewPassword
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await client.PostAsync("users/forgot-password", content);
                if (!response.IsSuccessStatusCode)
                {
                    ModelState.AddModelError("", "Tài khoản không tồn tại trên hệ thống!");
                    return View(model);
                }

                TempData["SuccessMessage"] = "Đặt lại mật khẩu thành công! Hãy đăng nhập bằng mật khẩu mới.";
                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Lỗi kết nối: {ex.Message}");
                return View(model);
            }
        }

        // HÀM TRỢ GIÚP: Truy vấn đơn hàng của khách từ API
        private async Task<List<CustomerOrderHistoryDto>> FetchCustomerOrdersAsync(ProfileViewModel profile)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("StoreAPI");
                string queryParam = !string.IsNullOrWhiteSpace(profile.PhoneNumber)
                    ? $"customerPhone={Uri.EscapeDataString(profile.PhoneNumber.Trim())}"
                    : (!string.IsNullOrWhiteSpace(profile.FullName) ? $"search={Uri.EscapeDataString(profile.FullName.Trim())}" : "");

                var response = await client.GetAsync($"orders{(string.IsNullOrEmpty(queryParam) ? "" : $"?{queryParam}")}");

                if (response.IsSuccessStatusCode)
                {
                    var ordersJson = await response.Content.ReadAsStringAsync();
                    var allOrders = JsonSerializer.Deserialize<List<CustomerOrderHistoryDto>>(ordersJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (allOrders != null && allOrders.Any())
                    {
                        return allOrders.Where(o =>
                            (!string.IsNullOrWhiteSpace(profile.PhoneNumber) && o.CustomerPhone != null && o.CustomerPhone.Contains(profile.PhoneNumber.Trim())) ||
                            (!string.IsNullOrWhiteSpace(profile.Email) && o.CustomerPhone != null && o.CustomerPhone.Equals(profile.Email.Trim(), StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrWhiteSpace(profile.FullName) && o.CustomerName != null && o.CustomerName.Equals(profile.FullName.Trim(), StringComparison.OrdinalIgnoreCase))
                        )
                        .OrderByDescending(o => o.OrderId)
                        .ToList();
                    }
                }
            }
            catch { }

            return new List<CustomerOrderHistoryDto>();
        }
    }
}