using ComputerStore.Application.DTOs.Products;
using ComputerStore.Application.Exceptions;
using ComputerStore.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ComputerStore.API.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductsApiController : ControllerBase
    {
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

        [HttpGet]
        public Task<IActionResult> GetProducts([FromQuery] ProductQueryDto filter,
            CancellationToken cancellationToken) =>
            ExecuteAsync(async () => Ok(await _queries.GetAllAsync(filter, cancellationToken)));

        [HttpGet("paged")]
        public Task<IActionResult> GetPagedProducts([FromQuery] ProductQueryDto filter,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 8,
            CancellationToken cancellationToken = default) =>
            ExecuteAsync(async () => Ok(await _queries.GetPageAsync(filter, page, pageSize, cancellationToken)));

        [HttpGet("suggest")]
        public Task<IActionResult> Suggest([FromQuery] string? term, CancellationToken cancellationToken) =>
            ExecuteAsync(async () => Ok(await _queries.SuggestAsync(term, cancellationToken)));

        [HttpGet("{id:int}")]
        public Task<IActionResult> GetProduct(int id, CancellationToken cancellationToken) =>
            ExecuteAsync(async () =>
            {
                var product = await _queries.GetByIdAsync(id, cancellationToken);
                if (product == null) return NotFound(new { Message = "Không tìm thấy sản phẩm." });
                return Ok(product);
            });

        [HttpPost]
        public Task<IActionResult> CreateProduct([FromBody] ProductCreateDto request,
            CancellationToken cancellationToken) =>
            ExecuteAsync(async () =>
            {
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
                await _commands.UpdateAsync(id, request, cancellationToken);
                return Ok(new { Message = "Cập nhật sản phẩm thành công." });
            });

        [HttpDelete("{id:int}")]
        public Task<IActionResult> DeleteProduct(int id, CancellationToken cancellationToken) =>
            ExecuteAsync(async () =>
            {
                await _commands.DeleteAsync(id, cancellationToken);
                return Ok(new { Message = "Xóa sản phẩm thành công." });
            });

        private async Task<IActionResult> ExecuteAsync(Func<Task<IActionResult>> action)
        {
            try
            {
                return await action();
            }
            catch (ProductOperationException ex)
            {
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
                _logger.LogError(ex, "Không thể xử lý yêu cầu sản phẩm.");
                return Problem(statusCode: 500, title: "Không thể xử lý sản phẩm. Vui lòng thử lại sau.");
            }
        }
    }
}
