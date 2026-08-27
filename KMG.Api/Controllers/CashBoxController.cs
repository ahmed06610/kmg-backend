using KMG.Api.Authorization;
using KMG.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace KMG.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CashBoxController : ControllerBase
    {
        private readonly ICashBoxService _cashBoxService;

        public CashBoxController(ICashBoxService cashBoxService)
        {
            _cashBoxService = cashBoxService;
        }

        [HttpGet]
        [AuthorizeAbility("عرض الخزنة")]
        public async Task<IActionResult> GetDetails([FromQuery] int recentCount = 50)
        {
            return Ok(await _cashBoxService.GetDetailsAsync(recentCount));
        }
    }
}
