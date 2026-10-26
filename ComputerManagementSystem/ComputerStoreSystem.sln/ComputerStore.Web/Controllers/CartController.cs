using ComputerStore.Core.Entities;
using ComputerStore.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ComputerStore.Web.Controllers
{
    public class CartDtoItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public int Quantity { get; set; }
    }

    public class CartController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public CartController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // HÀM LẤY SESSION KEY RIÊNG BIỆT THEO TỪNG TÀI KHOẢN ĐĂNG NHẬP
        private string GetCartSessionKey()
        {
            var userJson = HttpContext.Session.GetString(AccountController.UserSessionKey);
            if (!string.IsNullOrEmpty(userJson))
            {
                try
                {
                    var profile = JsonSerializer.Deserialize<ProfileViewModel>(userJson);
                    if (profile != null && !string.IsNullOrWhiteSpace(profile.PhoneNumber))
                    {
                        return $"Cart_{profile.PhoneNumber.Trim()}";
                    }
                }
                catch { }
            }
            return "Cart_Guest"; // Khách vãng lai chưa đăng nhập
        }

        private async Task<Product?> GetProductFromApiAsync(int productId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("StoreAPI");
                var response = await client.GetAsync($"products/{productId}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<Product>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
            }
            catch { }
            return null;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var dtoList = GetDtoList();

            if (dtoList.Any(x => x.Price <= 0 || string.IsNullOrWhiteSpace(x.ProductName) || x.ProductName == "Laptop Gaming"))
            {
                try
                {
                    var client = _httpClientFactory.CreateClient("StoreAPI");
                    var response = await client.GetAsync("products");
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var allProducts = JsonSerializer.Deserialize<List<Product>>(json, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (allProducts != null)
                        {
                            bool updated = false;
                            foreach (var item in dtoList.Where(x => x.Price <= 0 || string.IsNullOrWhiteSpace(x.ProductName) || x.ProductName == "Laptop Gaming"))
                            {
                                var prod = allProducts.FirstOrDefault(p => p.ProductId == item.ProductId);
                                if (prod != null)
                                {
                                    item.ProductName = prod.Name;
                                    item.Price = prod.Price;
                                    if (!string.IsNullOrEmpty(prod.ImageUrl))
                                    {
                                        item.ImageUrl = prod.ImageUrl;
                                    }
                                    updated = true;
                                }
                            }
                            if (updated) SaveDtoList(dtoList);
                        }
                    }
                }
                catch { }
            }

            var cartItems = ConvertToCartItems(dtoList);
            return View(cartItems);
        }

        // THÊM SẢN PHẨM VÀO GIỎ HÀNG (LƯU THEO TÀI KHOẢN)
        [HttpPost]
        [HttpGet]
        public async Task<IActionResult> AddToCart(int? productId, int? id, string? productName, decimal? price, string? imageUrl, int quantity = 1, string? returnUrl = null, bool buyNow = false)
        {
            int targetId = productId ?? id ?? 0;
            if (targetId <= 0) return RedirectToAction("Index", "Product");

            // 1. KIỂM TRA ĐĂNG NHẬP QUA SESSION
            var userJson = HttpContext.Session.GetString(AccountController.UserSessionKey);
            if (string.IsNullOrEmpty(userJson))
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập tài khoản BMDTStore để có thể mua sản phẩm.";
                var redirectUrl = !string.IsNullOrEmpty(returnUrl) ? returnUrl : $"/Product/Detail/{targetId}";
                return RedirectToAction("Login", "Account", new { returnUrl = redirectUrl });
            }

            // 2. XỬ LÝ LẤY DỮ LIỆU SẢN PHẨM & KIỂM TRA TỒN KHO
            var productInfo = await GetProductFromApiAsync(targetId);

            decimal finalPrice = (price.HasValue && price.Value > 0) ? price.Value : (productInfo?.Price ?? 0m);
            string finalName = !string.IsNullOrWhiteSpace(productName) ? productName : (productInfo?.Name ?? "Laptop Gaming");
            string finalImg = !string.IsNullOrWhiteSpace(imageUrl) ? imageUrl : (productInfo?.ImageUrl ?? "");
            int availableStock = productInfo?.StockQuantity ?? int.MaxValue;

            if (availableStock <= 0)
            {
                TempData["ErrorMessage"] = $"Sản phẩm '{finalName}' hiện đã hết hàng!";
                return RedirectToAction(nameof(Index));
            }

            // 3. LƯU VÀO GIỎ HÀNG RIÊNG CỦA TÀI KHOẢN
            var dtoList = GetDtoList();
            var item = dtoList.FirstOrDefault(x => x.ProductId == targetId);

            int addQty = quantity > 0 ? quantity : 1;

            if (item != null)
            {
                if (item.Quantity + addQty > availableStock)
                {
                    item.Quantity = availableStock;
                    TempData["ErrorMessage"] = $"Sản phẩm '{finalName}' chỉ còn {availableStock} máy trong kho. Đã điều chỉnh về số lượng tối đa.";
                }
                else
                {
                    item.Quantity += addQty;
                }

                if (item.Price <= 0 && finalPrice > 0) item.Price = finalPrice;
                if (item.ProductName == "Laptop Gaming" && finalName != "Laptop Gaming") item.ProductName = finalName;
            }
            else
            {
                if (addQty > availableStock)
                {
                    addQty = availableStock;
                    TempData["ErrorMessage"] = $"Sản phẩm '{finalName}' chỉ còn {availableStock} máy trong kho. Đã lấy số lượng tối đa.";
                }

                dtoList.Add(new CartDtoItem
                {
                    ProductId = targetId,
                    ProductName = finalName,
                    Price = finalPrice,
                    ImageUrl = finalImg,
                    Quantity = addQty
                });
            }

            SaveDtoList(dtoList);

            if (buyNow)
            {
                return RedirectToAction("Checkout");
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> UpdateQuantity(int productId, int quantity)
        {
            if (productId <= 0) return RedirectToAction(nameof(Index));

            var dtoList = GetDtoList();
            var item = dtoList.FirstOrDefault(x => x.ProductId == productId);

            if (item != null)
            {
                if (quantity <= 0)
                {
                    dtoList.Remove(item);
                }
                else
                {
                    var prod = await GetProductFromApiAsync(productId);
                    int stock = prod?.StockQuantity ?? int.MaxValue;

                    if (quantity > stock)
                    {
                        item.Quantity = stock;
                        TempData["ErrorMessage"] = $"Sản phẩm '{item.ProductName}' chỉ còn {stock} máy trong kho.";
                    }
                    else
                    {
                        item.Quantity = quantity;
                    }
                }
                SaveDtoList(dtoList);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult RemoveFromCart(int productId)
        {
            if (productId <= 0) return RedirectToAction(nameof(Index));

            var dtoList = GetDtoList();
            var item = dtoList.FirstOrDefault(x => x.ProductId == productId);

            if (item != null)
            {
                dtoList.Remove(item);
                SaveDtoList(dtoList);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult ClearCart()
        {
            HttpContext.Session.Remove(GetCartSessionKey());
            return RedirectToAction(nameof(Index));
        }

        // GET: /Cart/Checkout - Trang thanh toán
        [HttpGet]
        public IActionResult Checkout()
        {
            var userJson = HttpContext.Session.GetString(AccountController.UserSessionKey);
            if (string.IsNullOrEmpty(userJson))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để tiến hành đặt hàng.";
                return RedirectToAction("Login", "Account", new { returnUrl = "/Cart/Checkout" });
            }

            var dtoList = GetDtoList();
            if (!dtoList.Any()) return RedirectToAction(nameof(Index));

            var cartItems = ConvertToCartItems(dtoList);
            return View(cartItems);
        }

        // POST: /Cart/Checkout - LƯU ĐƠN HÀNG
        [HttpPost]
        public async Task<IActionResult> Checkout(string customerName, string customerPhone, string shippingAddress, string paymentMethod = "COD")
        {
            var userJson = HttpContext.Session.GetString(AccountController.UserSessionKey);
            if (string.IsNullOrEmpty(userJson))
            {
                TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account", new { returnUrl = "/Cart/Checkout" });
            }

            var dtoList = GetDtoList();
            if (!dtoList.Any())
            {
                return RedirectToAction("Index", "Product");
            }

            string sessionUsername = "";
            string sessionFullName = "";
            string sessionPhone = "";

            try
            {
                using var doc = JsonDocument.Parse(userJson);
                var root = doc.RootElement;
                if (root.TryGetProperty("Username", out var u)) sessionUsername = u.GetString() ?? "";
                if (root.TryGetProperty("FullName", out var fn)) sessionFullName = fn.GetString() ?? "";
                if (root.TryGetProperty("PhoneNumber", out var ph)) sessionPhone = ph.GetString() ?? "";
            }
            catch { }

            string finalName = !string.IsNullOrWhiteSpace(customerName) ? customerName.Trim() : (!string.IsNullOrWhiteSpace(sessionFullName) ? sessionFullName : "Khách hàng");
            string finalPhone = !string.IsNullOrWhiteSpace(customerPhone) ? customerPhone.Trim() : (!string.IsNullOrWhiteSpace(sessionPhone) ? sessionPhone : sessionUsername);
            string rawAddress = !string.IsNullOrWhiteSpace(shippingAddress) ? shippingAddress.Trim() : "Địa chỉ mặc định theo tài khoản";

            string paymentText = paymentMethod switch
            {
                "BANKING" => "Chuyển khoản (VietQR 24/7)",
                "VNPAY" => "VNPAY / Thẻ ATM",
                _ => "Thanh toán khi nhận hàng (COD)"
            };

            string finalDeliveryAddress = $"{rawAddress} [PTTT: {paymentText}]";

            try
            {
                var client = _httpClientFactory.CreateClient("StoreAPI");

                var orderPayload = new
                {
                    Username = sessionUsername,
                    CustomerName = finalName,
                    CustomerPhone = finalPhone,
                    DeliveryAddress = finalDeliveryAddress,
                    CustomerAddress = rawAddress,
                    PaymentMethod = paymentText,
                    OrderDate = DateTime.UtcNow.AddHours(7),
                    TotalAmount = dtoList.Sum(x => x.Price * x.Quantity),
                    Status = "Chờ xử lý",
                    OrderDetails = dtoList.Select(item => new
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity
                    }).ToList()
                };

                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(orderPayload),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await client.PostAsync("orders", jsonContent);
                HttpResponseMessage finalResponse = response;

                if (!response.IsSuccessStatusCode)
                {
                    var fallbackResponse = await client.PostAsync("Orders/create", jsonContent);
                    if (!fallbackResponse.IsSuccessStatusCode)
                    {
                        var errorResp = await response.Content.ReadAsStringAsync();
                        ViewBag.Error = $"Đặt hàng không thành công: {errorResp}";
                        var items = ConvertToCartItems(dtoList);
                        return View(items);
                    }
                    finalResponse = fallbackResponse;
                }

                string createdOrderId = "";
                try
                {
                    var respJson = await finalResponse.Content.ReadAsStringAsync();
                    using var respDoc = JsonDocument.Parse(respJson);
                    if (respDoc.RootElement.TryGetProperty("orderId", out var oidProp) ||
                        respDoc.RootElement.TryGetProperty("OrderId", out oidProp))
                    {
                        createdOrderId = $" #{oidProp}";
                    }
                }
                catch { }

                // Xóa giỏ hàng riêng của tài khoản sau khi đặt thành công
                HttpContext.Session.Remove(GetCartSessionKey());

                TempData["OrderSuccessToast"] = $"Đặt hàng thành công! Đơn hàng{createdOrderId} của bạn đang được xử lý.";
                return RedirectToAction("Index", "Product");
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Lỗi khi kết nối API đặt hàng: {ex.Message}";
                var cartItems = ConvertToCartItems(dtoList);
                return View(cartItems);
            }
        }

        // --- CÁC HÀM PHỤ TRỢ NỘI BỘ ---
        private List<CartDtoItem> GetDtoList()
        {
            var json = HttpContext.Session.GetString(GetCartSessionKey());
            if (string.IsNullOrEmpty(json)) return new List<CartDtoItem>();

            try
            {
                return JsonSerializer.Deserialize<List<CartDtoItem>>(json) ?? new List<CartDtoItem>();
            }
            catch
            {
                return new List<CartDtoItem>();
            }
        }

        private void SaveDtoList(List<CartDtoItem> list)
        {
            HttpContext.Session.SetString(GetCartSessionKey(), JsonSerializer.Serialize(list));
        }

        private List<CartItem> ConvertToCartItems(List<CartDtoItem> dtoList)
        {
            var result = new List<CartItem>();
            var itemType = typeof(CartItem);

            foreach (var dto in dtoList)
            {
                var item = new CartItem();

                itemType.GetProperty("ProductId")?.SetValue(item, dto.ProductId);
                itemType.GetProperty("Id")?.SetValue(item, dto.ProductId);
                itemType.GetProperty("ProductName")?.SetValue(item, dto.ProductName);
                itemType.GetProperty("Name")?.SetValue(item, dto.ProductName);
                itemType.GetProperty("Price")?.SetValue(item, dto.Price);
                itemType.GetProperty("UnitPrice")?.SetValue(item, dto.Price);
                itemType.GetProperty("ImageUrl")?.SetValue(item, dto.ImageUrl);
                itemType.GetProperty("Image")?.SetValue(item, dto.ImageUrl);
                itemType.GetProperty("Quantity")?.SetValue(item, dto.Quantity);

                var pProp = itemType.GetProperty("Product");
                if (pProp != null)
                {
                    var prod = new Product
                    {
                        ProductId = dto.ProductId,
                        Name = dto.ProductName,
                        Price = dto.Price,
                        ImageUrl = dto.ImageUrl
                    };
                    pProp.SetValue(item, prod);
                }

                result.Add(item);
            }

            return result;
        }
    }
}