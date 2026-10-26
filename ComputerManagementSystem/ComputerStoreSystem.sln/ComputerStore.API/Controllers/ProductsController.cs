using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ComputerStore.Core.Entities;
using ComputerStore.Infrastructure.Data;

namespace ComputerStore.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/products (Bộ lọc đa điều kiện chặt chẽ)
        [HttpGet]
        public async Task<IActionResult> GetProducts(
            [FromQuery] string? query,
            [FromQuery] int? categoryId,
            [FromQuery] string? brand,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] string? sortBy)
        {
            try
            {
                var q = _context.Products.Include(p => p.Category).AsNoTracking().AsQueryable();

                // 1. Ràng buộc từ khóa (tìm cả trong tên, mô tả và cấu hình)
                if (!string.IsNullOrWhiteSpace(query))
                {
                    var keyword = query.Trim().ToLower();
                    q = q.Where(p => p.Name.ToLower().Contains(keyword) ||
                                     (p.Specifications != null && p.Specifications.ToLower().Contains(keyword)));
                }

                // 2. Ràng buộc Danh mục
                if (categoryId.HasValue && categoryId.Value > 0)
                {
                    q = q.Where(p => p.CategoryId == categoryId.Value);
                }

                // 3. Ràng buộc Thương hiệu (Brand)
                if (!string.IsNullOrWhiteSpace(brand))
                {
                    var brandTerm = brand.Trim().ToLower();
                    q = q.Where(p => p.Name.ToLower().Contains(brandTerm));
                }

                // 4. Ràng buộc khoảng giá
                if (minPrice.HasValue && minPrice.Value < 0) minPrice = 0;
                if (maxPrice.HasValue && maxPrice.Value < 0) maxPrice = 0;

                if (minPrice.HasValue && maxPrice.HasValue && minPrice.Value > maxPrice.Value)
                {
                    var temp = minPrice;
                    minPrice = maxPrice;
                    maxPrice = temp;
                }

                if (minPrice.HasValue && minPrice.Value > 0)
                {
                    q = q.Where(p => p.Price >= minPrice.Value);
                }

                if (maxPrice.HasValue && maxPrice.Value > 0)
                {
                    q = q.Where(p => p.Price <= maxPrice.Value);
                }

                // 5. Sắp xếp
                q = sortBy switch
                {
                    "price_asc" => q.OrderBy(p => p.Price),
                    "price_desc" => q.OrderByDescending(p => p.Price),
                    "name_asc" => q.OrderBy(p => p.Name),
                    _ => q.OrderByDescending(p => p.ProductId)
                };

                var list = await q.ToListAsync();
                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi truy vấn danh sách sản phẩm: {ex.Message}");
            }
        }

        // GET: api/products/suggest?term=...
        [HttpGet("suggest")]
        public async Task<IActionResult> Suggest([FromQuery] string? term)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(term) || term.Trim().Length < 1)
                {
                    return Ok(new List<object>());
                }

                var keyword = term.Trim().ToLower();
                var suggestions = await _context.Products
                    .AsNoTracking()
                    .Where(p => p.Name.ToLower().Contains(keyword) ||
                                (p.Specifications != null && p.Specifications.ToLower().Contains(keyword)))
                    .Take(15)
                    .Select(p => new
                    {
                        p.ProductId,
                        p.Name,
                        p.Price,
                        p.ImageUrl,
                        p.StockQuantity
                    })
                    .ToListAsync();

                return Ok(suggestions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi gợi ý tìm kiếm: {ex.Message}");
            }
        }

        // GET: api/products/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.ProductId == id);

                if (product == null) return NotFound($"Không tìm thấy sản phẩm #{id}");
                return Ok(product);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi truy vấn sản phẩm #{id}: {ex.Message}");
            }
        }

        // POST: api/products - Thêm mới sản phẩm vào kho
        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] Product product)
        {
            try
            {
                if (product == null) return BadRequest("Dữ liệu sản phẩm không hợp lệ.");

                product.ProductId = 0; // Để SQL Identity tự sinh mã
                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetProduct), new { id = product.ProductId }, product);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi thêm mới sản phẩm: {ex.Message}");
            }
        }

        // PUT: api/products/{id} - Cập nhật thông tin & số lượng tồn kho
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductUpdateDto dto)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return NotFound($"Không tìm thấy sản phẩm #{id} cần cập nhật.");
                }

                product.Name = dto.Name?.Trim() ?? product.Name;
                product.Price = dto.Price;
                product.StockQuantity = dto.StockQuantity;
                product.ImageUrl = dto.ImageUrl;
                if (!string.IsNullOrWhiteSpace(dto.Description))
                {
                    product.Description = dto.Description.Trim();
                    product.Specifications = dto.Description.Trim();
                }
                if (dto.CategoryId.HasValue && dto.CategoryId.Value > 0)
                {
                    product.CategoryId = dto.CategoryId.Value;
                }

                await _context.SaveChangesAsync();
                return Ok(new { Message = $"Đã cập nhật thông tin và kho cho máy #{id} thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi cập nhật sản phẩm #{id}: {ex.Message}");
            }
        }

        // DELETE: api/products/{id} - Xóa sản phẩm khỏi kho
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return NotFound($"Không tìm thấy sản phẩm #{id} cần xóa.");
                }

                // Kiểm tra xem sản phẩm đã có trong đơn hàng nào chưa
                bool hasOrder = await _context.OrderDetails.AnyAsync(od => od.ProductId == id);
                if (hasOrder)
                {
                    return BadRequest("Không thể xóa vì sản phẩm này đã phát sinh trong các đơn hàng đã đặt.");
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                return Ok(new { Message = $"Đã xóa sản phẩm #{id} thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi xóa sản phẩm #{id}: {ex.Message}");
            }
        }
    }

    public class ProductUpdateDto
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public int? CategoryId { get; set; }
    }
}