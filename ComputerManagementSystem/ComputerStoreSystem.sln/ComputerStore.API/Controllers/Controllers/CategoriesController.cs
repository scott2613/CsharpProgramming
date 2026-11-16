using ComputerStore.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
namespace ComputerStore.API.Controllers;
// Khai báo controller API với tuyến GET /api/categories để lấy danh sách danh mục.
[ApiController]
[Route("api/categories")]
public sealed class CategoriesController(IProductQueryService products) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken) =>
        // Lấy danh mục từ tầng truy vấn và trả về HTTP 200.
        Ok(await products.GetCategoriesAsync(cancellationToken));
}
