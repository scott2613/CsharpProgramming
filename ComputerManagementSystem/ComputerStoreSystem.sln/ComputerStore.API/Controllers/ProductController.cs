using ComputerStore.Application.DTOs.Products;
using ComputerStore.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ComputerStore.API.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductQueryService _products;
        private readonly ILogger<ProductController> _logger;

        public ProductController(
            IProductQueryService products,
            ILogger<ProductController> logger)
        {
            _products = products;
            _logger = logger;
        }

        // GET: /Product
        // GET: /Product/Index
        [HttpGet]
        public async Task<IActionResult> Index(
            string? query,
            int? categoryId,
            string? brand,
            decimal? minPrice,
            decimal? maxPrice,
            string? sortBy,
            int page = 1,
            CancellationToken cancellationToken = default)
        {
            var filter = new ProductQueryDto
            {
                Query = query,
                CategoryId = categoryId,
                Brand = brand,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                SortBy = sortBy
            };

            ProductPageDto result;

            try
            {
                ViewBag.Categories = await _products.GetCategoriesAsync(cancellationToken);
                result = await _products.GetPageAsync(
                    filter,
                    page,
                    8,
                    cancellationToken);
            }
            catch (ArgumentException ex)
            {
                TempData["ErrorMessage"] = ex.Message;

                result = new ProductPageDto
                {
                    Filter = filter
                };
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(
                    ex,
                    "Không tải được danh sách sản phẩm.");

                TempData["ErrorMessage"] =
                    "Không tải được sản phẩm. Vui lòng thử lại sau.";

                result = new ProductPageDto
                {
                    Filter = filter
                };
            }

            ViewBag.CurrentQuery = result.Filter.Query;
            ViewBag.CurrentCategory = result.Filter.CategoryId;
            ViewBag.CurrentBrand = result.Filter.Brand;
            ViewBag.CurrentMinPrice = result.Filter.MinPrice;
            ViewBag.CurrentMaxPrice = result.Filter.MaxPrice;
            ViewBag.CurrentSortBy = result.Filter.SortBy;

            ViewBag.CurrentPage = result.Page;
            ViewBag.CurrentTotalPages = result.TotalPages;
            ViewBag.TotalPages = result.TotalPages;
            ViewBag.TotalItems = result.TotalItems;

            return View(result.Items);
        }

        // GET: /Product/Suggest?term=...
        [HttpGet]
        public async Task<IActionResult> Suggest(
            string? term,
            CancellationToken cancellationToken)
        {
            try
            {
                var suggestions = await _products.SuggestAsync(
                    term,
                    cancellationToken);

                return Json(suggestions);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    Message = ex.Message
                });
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(
                    ex,
                    "Không tải được gợi ý sản phẩm.");

                return StatusCode(503, new
                {
                    Message = "Chưa thể tải gợi ý sản phẩm."
                });
            }
        }

        // Giữ tương thích với đường dẫn /Product/Details/1
        [HttpGet]
        public Task<IActionResult> Details(
            int id,
            CancellationToken cancellationToken)
        {
            return Detail(id, cancellationToken);
        }

        // GET: /Product/Detail/1
        [HttpGet]
        public async Task<IActionResult> Detail(
            int id,
            CancellationToken cancellationToken)
        {
            try
            {
                var product = await _products.GetByIdAsync(
                    id,
                    cancellationToken);

                if (product == null)
                {
                    return NotFound("Không tìm thấy sản phẩm.");
                }

                return View(
                    "~/Views/Product/Detail.cshtml",
                    product);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(
                    ex,
                    "Không tải được sản phẩm {ProductId}.",
                    id);

                return StatusCode(
                    503,
                    "Không thể tải sản phẩm. Vui lòng thử lại sau.");
            }
        }
    }
}