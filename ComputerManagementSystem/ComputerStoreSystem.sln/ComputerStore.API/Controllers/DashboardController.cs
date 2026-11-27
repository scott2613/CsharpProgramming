using ComputerStore.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ComputerStore.API.Security;
namespace ComputerStore.API.Controllers;
[ApiController,Route("api/dashboard"),Authorize(Roles="Admin")]
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class DashboardController(IDashboardService service) : ControllerBase
{
 [HttpGet]
 public async Task<IActionResult> Get([FromQuery] DateTime? from,[FromQuery] DateTime? to,CancellationToken ct)
 {
  if(!from.HasValue||!to.HasValue) return BadRequest(new{Message="Cần truyền from và to theo yyyy-MM-dd."});
  return Ok(await service.GetAsync(AccountIdentity.Id(User),from.Value,to.Value,ct));
 }
}
