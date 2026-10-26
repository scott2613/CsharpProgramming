using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using ComputerStore.Core.Entities;

namespace ComputerStore.Web.Controllers
{
    public class ProductController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ProductController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? query,
            int? categoryId,
            string? brand,
            decimal? minPrice,
            decimal? maxPrice,
            string? sortBy,
            int page = 1)
        {
            var client = _httpClientFactory.CreateClient("StoreAPI");
            var allProducts = new List<Product>();

            // Ràng buộc khoảng giá hợp lệ
            if (minPrice.HasValue && minPrice.Value < 0) minPrice = 0;
            if (maxPrice.HasValue && maxPrice.Value < 0) maxPrice = 0;
            if (minPrice.HasValue && maxPrice.HasValue && minPrice > maxPrice)
            {
                var temp = minPrice;
                minPrice = maxPrice;
                maxPrice = temp;
            }

            ViewBag.CurrentQuery = query;
            ViewBag.CurrentCategory = categoryId;
            ViewBag.CurrentBrand = brand;
            ViewBag.CurrentMinPrice = minPrice;
            ViewBag.CurrentMaxPrice = maxPrice;
            ViewBag.CurrentSortBy = sortBy;

            try
            {
                var encodedQuery = Uri.EscapeDataString(query ?? "");
                var encodedBrand = Uri.EscapeDataString(brand ?? "");
                var url = $"products?query={encodedQuery}&categoryId={categoryId}&brand={encodedBrand}&minPrice={minPrice}&maxPrice={maxPrice}&sortBy={sortBy}";

                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    allProducts = JsonSerializer.Deserialize<List<Product>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<Product>();
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi kết nối máy chủ dữ liệu: {ex.Message}";
            }

            // Phân trang: 8 sản phẩm mỗi trang
            int pageSize = 8;
            int totalItems = allProducts.Count;
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var pagedProducts = allProducts
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.CurrentTotalPages = totalPages;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(pagedProducts);
        }

        [HttpGet]
        public async Task<IActionResult> Suggest(string term)
        {
            var client = _httpClientFactory.CreateClient("StoreAPI");
            try
            {
                var response = await client.GetAsync($"products/suggest?term={Uri.EscapeDataString(term ?? "")}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return Content(content, "application/json");
                }
            }
            catch
            {
            }
            return Json(new object[] { });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id) => await Detail(id);

        [HttpGet]
        [ActionName("Detail")]
        public async Task<IActionResult> Detail(int id)
        {
            if (id <= 0) return RedirectToAction(nameof(Index));

            var client = _httpClientFactory.CreateClient("StoreAPI");
            Product? product = null;

            try
            {
                var response = await client.GetAsync($"products/{id}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    product = JsonSerializer.Deserialize<Product>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi nạp chi tiết sản phẩm: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }

            if (product == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin sản phẩm yêu cầu.";
                return RedirectToAction(nameof(Index));
            }

            return View("~/Views/Product/Detail.cshtml", product);
        }
    }
}