using ComputerStore.Application.DTOs.Products;
using ComputerStore.Application.Exceptions;
using ComputerStore.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ComputerStore.API.Controllers
{
    [ApiController]
    // Tiền tố chung cho các API truy vấn và cập nhật sản phẩm.
    [Route("api/products")]
    public class ProductsApiController : ControllerBase
    {
        // Tách service đọc sản phẩm và service xử lý thao tác ghi.
        private readonly IProductQueryService _queries;
        private readonly IProductCommandService _commands;
        private readonly ILogger<ProductsApiController> _logger;

        public ProductsApiController(IProductQueryService queries,
            IProductCommandService commands, ILogger<ProductsApiController> logger)
        {
            _queries = queries;
            _commands = commands;
            _logger = logger;
        }

        // Trả danh sách sản phẩm áp dụng bộ lọc từ chuỗi truy vấn.
        [HttpGet]
        public Task<IActionResult> GetProducts([FromQuery] ProductQueryDto filter,
            CancellationToken cancellationToken) =>
            ExecuteAsync(async () => Ok(await _queries.GetAllAsync(filter, cancellationToken)));

        // Trả kết quả theo trang và kích thước trang được yêu cầu.
        [HttpGet("paged")]
        public Task<IActionResult> GetPagedProducts([FromQuery] ProductQueryDto filter,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 8,
            CancellationToken cancellationToken = default) =>
            ExecuteAsync(async () => Ok(await _queries.GetPageAsync(filter, page, pageSize, cancellationToken)));

        // Cung cấp danh sách gợi ý theo từ khóa tìm kiếm.
        [HttpGet("suggest")]
        public Task<IActionResult> Suggest([FromQuery] string? term, CancellationToken cancellationToken) =>
            ExecuteAsync(async () => Ok(await _queries.SuggestAsync(term, cancellationToken)));

        [HttpGet("{id:int}")]
        public Task<IActionResult> GetProduct(int id, CancellationToken cancellationToken) =>
            ExecuteAsync(async () =>
            {
                var product = await _queries.GetByIdAsync(id, cancellationToken);
                // Trả HTTP 404 khi không tìm thấy sản phẩm theo mã.
                if (product == null) return NotFound(new { Message = "Không tìm thấy sản phẩm." });
                return Ok(product);
            });

        [HttpPost]
        public Task<IActionResult> CreateProduct([FromBody] ProductCreateDto request,
            CancellationToken cancellationToken) =>
            ExecuteAsync(async () =>
            {
                // Tạo sản phẩm và trả HTTP 201 kèm đường dẫn tra cứu mã vừa tạo.
                int id = await _commands.CreateAsync(request, cancellationToken);
                return CreatedAtAction(nameof(GetProduct), new { id }, new
                {
                    ProductId = id,
                    Message = "Thêm sản phẩm thành công."
                });
            });

        [HttpPut("{id:int}")]
        public Task<IActionResult> UpdateProduct(int id, [FromBody] ProductUpdateDto request,
            CancellationToken cancellationToken) =>
            ExecuteAsync(async () =>
            {
                // Áp dụng dữ liệu cập nhật cho sản phẩm được xác định bởi mã trên URL.
                await _commands.UpdateAsync(id, request, cancellationToken);
                return Ok(new { Message = "Cập nhật sản phẩm thành công." });
            });

        [HttpDelete("{id:int}")]
        public Task<IActionResult> DeleteProduct(int id, CancellationToken cancellationToken) =>
            ExecuteAsync(async () =>
            {
                // Giao việc kiểm tra điều kiện xóa và xử lý xóa cho service.
                await _commands.DeleteAsync(id, cancellationToken);
                return Ok(new { Message = "Xóa sản phẩm thành công." });
            });

        // Dùng chung cách bắt lỗi và chuyển lỗi nghiệp vụ thành mã HTTP.
        private async Task<IActionResult> ExecuteAsync(Func<Task<IActionResult>> action)
        {
            try
            {
                return await action();
            }
            catch (ProductOperationException ex)
            {
                // Ánh xạ thiếu sản phẩm thành 404, xung đột thành 409 và lỗi nghiệp vụ khác thành 400.
                int status = ex.Kind switch
                {
                    ProductErrorKind.NotFound => 404,
                    ProductErrorKind.Conflict => 409,
                    _ => 400
                };
                return StatusCode(status, new { Message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Ghi chi tiết lỗi bất ngờ vào log và trả thông báo chung cho client.
                _logger.LogError(ex, "Không thể xử lý yêu cầu sản phẩm.");
                return Problem(statusCode: 500, title: "Không thể xử lý sản phẩm. Vui lòng thử lại sau.");
            }
        }
    }
}
