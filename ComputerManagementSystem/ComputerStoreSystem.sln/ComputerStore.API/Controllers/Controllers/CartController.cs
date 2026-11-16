using System.Globalization;
using ComputerStore.API.Models;
using ComputerStore.API.Security;
using ComputerStore.Application.DTOs.Accounts;
using ComputerStore.Application.DTOs.Orders;
using ComputerStore.Application.Exceptions;
using ComputerStore.Application.Interfaces;
using ComputerStore.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComputerStore.API.Controllers;

// Giới hạn các thao tác giỏ hàng cho Admin và Customer, trừ action có AllowAnonymous.
[Authorize(Roles = StoreRoles.Admin + "," + StoreRoles.Customer)]
// Kiểm tra mã chống CSRF cho các yêu cầu thay đổi giỏ hoặc đặt hàng.
[AutoValidateAntiforgeryToken]
// Yêu cầu trình duyệt không lưu phản hồi chứa dữ liệu giỏ hàng cá nhân.
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CartController(ICartService carts, IOrderService orders, IAccountService accounts, ILogger<CartController> logger) : Controller
{
    private int UserId => AccountIdentity.Id(User);
    // Đọc giỏ theo tài khoản lấy từ phiên đăng nhập.
    private Task<List<ShoppingCartItem>> ReadCartAsync(CancellationToken ct) => carts.ReadAsync(UserId, ct);
    private async Task<List<CartItem>> LoadItemsAsync(List<ShoppingCartItem> cart, CancellationToken ct)
    {
        // Lấy thông tin sản phẩm theo danh sách mã rồi tra cứu qua từ điển.
        var products = (await orders.CartProductsAsync(cart.Select(x => x.ProductId).ToArray(), ct))
            .ToDictionary(p => p.ProductId);
        return cart.Select(x => new CartItem
        {
            Quantity = x.Quantity,
            // Giữ dòng giỏ để người dùng có thể xóa nếu sản phẩm đã ngừng bán.
            Product = products.GetValueOrDefault(x.ProductId) ?? new Product
            { ProductId = x.ProductId, Name = $"Sản phẩm #{x.ProductId} không còn bán", StockQuantity = 0 }
        }).ToList();
    }
    [HttpGet, AllowAnonymous]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        // Khách chưa đăng nhập được xem giao diện giỏ trống.
        if (User.Identity?.IsAuthenticated != true) return View(new List<CartItem>());
        var items = await LoadItemsAsync(await ReadCartAsync(ct), ct);
        // Cảnh báo nếu số lượng yêu cầu lớn hơn tồn kho hiện tại.
        if (items.Any(x => x.Quantity > x.Product.StockQuantity))
            TempData["ErrorMessage"] = "Một số sản phẩm đã hết hàng hoặc không đủ số lượng. Vui lòng điều chỉnh giỏ hàng.";
        return View(items);
    }

    // Tương thích các link GET cũ trên trang sản phẩm: chỉ hiển thị xác nhận,
    // tuyệt đối không sửa giỏ hàng bằng GET. Form POST cũ vẫn hoạt động.
    [HttpGet]
    public async Task<IActionResult> AddToCart(int? productId, int? id, int quantity = 1,
        string? returnUrl = null, bool buyNow = false, CancellationToken ct = default)
    {
        // Chấp nhận cả hai tên tham số mã sản phẩm để hỗ trợ đường dẫn và biểu mẫu cũ.
        var target = productId ?? id ?? 0;
        if (target <= 0 || quantity is < 1 or > 999) return BadRequest("Sản phẩm hoặc số lượng không hợp lệ.");
        var product = (await orders.CartProductsAsync(new[] { target }, ct)).FirstOrDefault();
        if (product == null || product.StockQuantity <= 0)
        {
            TempData["ErrorMessage"] = "Sản phẩm không còn bán hoặc đã hết hàng.";
            return RedirectToAction(nameof(Index));
        }
        return View("AddToCart", new CartAddViewModel
        {
            ProductId = target,
            ProductName = product.Name,
            Quantity = quantity,
            BuyNow = buyNow,
            ReturnUrl = !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : null
        });
    }

    // Dùng POST để thêm sản phẩm và xử lý chuyển đến trang đăng nhập ngay trong action.
    [HttpPost, ActionName("AddToCart"), AllowAnonymous]
    public async Task<IActionResult> AddToCartPost(int? productId, int? id, int quantity = 1,
        string? returnUrl = null, bool buyNow = false, CancellationToken ct = default)
    {
        // Chấp nhận cả hai tên tham số mã sản phẩm để hỗ trợ đường dẫn và biểu mẫu cũ.
        var target = productId ?? id ?? 0;
        if (target <= 0 || quantity is < 1 or > 999)
        {
            TempData["ErrorMessage"] = "Sản phẩm hoặc số lượng không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }
        // Form sản phẩm khi chưa đăng nhập vẫn chuyển về trang chi tiết sau login,
        // không để cookie middleware quay về GET AddToCart thiếu ProductId trong body.
        if (User.Identity?.IsAuthenticated != true)
        {
            TempData["ErrorMessage"] = "Bạn cần đăng nhập để mua sản phẩm.";
            var back = !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? returnUrl : Url.Action("Detail", "Product", new { id = target });
            return RedirectToAction("Login", "Account", new { returnUrl = back });
        }
        // Kiểm tra lại vai trò trước khi sửa giỏ vì action POST cho phép truy cập ẩn danh.
        if (!User.IsInRole(StoreRoles.Admin) && !User.IsInRole(StoreRoles.Customer)) return Forbid();
        var product = (await orders.CartProductsAsync(new[] { target }, ct)).FirstOrDefault();
        if (product == null || product.StockQuantity <= 0)
        {
            TempData["ErrorMessage"] = "Sản phẩm không còn bán hoặc đã hết hàng.";
            return RedirectToAction(nameof(Index));
        }
        // Giao kiểm tra và thêm số lượng vào giỏ cho service.
        try { await carts.AddAsync(UserId, target, quantity, ct); }
        catch (OrderOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
        // Mua ngay chuyển sang thanh toán. Thêm thông thường quay lại giỏ.
        return RedirectToAction(buyNow ? nameof(Checkout) : nameof(Index));
    }
    // Dùng chung cách báo lỗi nghiệp vụ và quay lại giỏ sau các thao tác cập nhật hoặc xóa.
    private async Task<IActionResult> ChangeCartAsync(Func<Task> change)
    {
        try { await change(); }
        catch (OrderOperationException ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }
    [HttpPost]
    public Task<IActionResult> UpdateQuantity(int productId, int quantity, CancellationToken ct) =>
        ChangeCartAsync(() => carts.UpdateAsync(UserId, productId, quantity, ct));
    [HttpPost]
    public Task<IActionResult> RemoveFromCart(int productId, CancellationToken ct) =>
        ChangeCartAsync(() => carts.RemoveAsync(UserId, productId, ct));
    [HttpPost]
    public Task<IActionResult> ClearCart(CancellationToken ct) =>
        ChangeCartAsync(() => carts.ClearAsync(UserId, ct));
    // Nạp hồ sơ để điền sẵn thông tin người nhận trên trang thanh toán.
    private async Task PrepareCustomerAsync(CancellationToken ct)
    {
        var a = await accounts.ProfileAsync(UserId, ct);
        ViewBag.Customer = AccountController.ToProfile(a);
    }
    [HttpGet]
    public async Task<IActionResult> Checkout(CancellationToken ct)
    {
        var cart = await ReadCartAsync(ct);
        if (cart.Count == 0) return RedirectToAction(nameof(Index));
        var items = await LoadItemsAsync(cart, ct);
        // Chặn vào trang thanh toán nếu lượng tồn hiện tại không đáp ứng giỏ.
        if (items.Any(i => i.Quantity > i.Product.StockQuantity))
        {
            TempData["ErrorMessage"] = "Tồn kho đã thay đổi. Vui lòng điều chỉnh số lượng trước khi thanh toán.";
            return RedirectToAction(nameof(Index));
        }
        await PrepareCustomerAsync(ct);
        return View(items);
    }
    [HttpPost, ActionName("Checkout")]
    public async Task<IActionResult> CheckoutPost(string? customerName, string? customerPhone,
        string? shippingAddress, string paymentMethod = "COD", string? expectedTotal = null, CancellationToken ct = default)
    {
        var cart = await ReadCartAsync(ct);
        if (cart.Count == 0) return RedirectToAction(nameof(Index));
        // Giữ lại dữ liệu người dùng đã nhập để hiển thị lại khi đặt hàng lỗi.
        ViewBag.CustomerName = customerName;
        ViewBag.CustomerPhone = customerPhone;
        ViewBag.ShippingAddress = shippingAddress;
        ViewBag.PaymentMethod = paymentMethod;
        try
        {
            // Chuỗi invariant từ hidden input: không phụ thuộc locale dấu phẩy/dấu chấm.
            if (!ModelState.IsValid || !decimal.TryParse(expectedTotal, NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out var total) || total < 0)
                throw new OrderOperationException("Thông tin thanh toán không hợp lệ. Vui lòng tải lại trang.");
            // Gửi giỏ hiện tại, thông tin giao nhận và tổng tiền khách đã thấy cho service đặt hàng.
            var result = await orders.PlaceCartAsync(UserId, new PlaceOrderRequest
            {
                CustomerName = customerName ?? "",
                CustomerPhone = customerPhone ?? "",
                DeliveryAddress = shippingAddress,
                PaymentMethod = paymentMethod,
                ExpectedTotal = total,
                OrderDetails = cart.Select(x => new OrderLineRequest { ProductId = x.ProductId, Quantity = x.Quantity }).ToList()
            }, ct);
            TempData["OrderSuccessToast"] = $"Đặt hàng thành công! Đơn hàng #{result.OrderId} của bạn đang được xử lý.";
            return RedirectToAction("Index", "Product");
        }
        catch (OrderOperationException ex) { ViewBag.Error = ex.Message; }
        // Cho phép lỗi hủy yêu cầu truyền tiếp thay vì hiển thị như lỗi đặt hàng.
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            logger.LogError(ex, "Không hoàn tất đặt hàng cho tài khoản {UserId}", UserId);
            // Không tự POST lại vào endpoint dự phòng: có thể tạo trùng đơn khi mất phản hồi.
            ViewBag.Error = "Chưa xác nhận được kết quả đặt hàng. Hãy kiểm tra mục Đơn hàng trước khi thử lại.";
        }
        await PrepareCustomerAsync(ct);
        // Đọc lại giỏ sau lỗi vì trạng thái có thể đã đổi trong lúc xử lý đơn.
        var remaining = await ReadCartAsync(ct);
        if (remaining.Count == 0)
        {
            TempData["ErrorMessage"] = ViewBag.Error as string ?? "Giỏ hàng đã trống. Hãy kiểm tra mục Đơn hàng.";
            return RedirectToAction(nameof(Index));
        }
        return View("Checkout", await LoadItemsAsync(remaining, ct));
    }
}
