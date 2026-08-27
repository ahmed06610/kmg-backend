using KMG.Api.Authorization;
using KMG.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace KMG.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet]
        [AuthorizeAbility("عرض لوحة التحكم")]
        public async Task<IActionResult> Get()
        {
            return Ok(await _dashboardService.GetDashboardAsync());
        }
    }
}
