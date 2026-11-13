using ComputerStore.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
namespace ComputerStore.API.Controllers;
[ApiController]
[Route("api/categories")]
public sealed class CategoriesController(IProductQueryService products) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken) =>
        Ok(await products.GetCategoriesAsync(cancellationToken));
}
