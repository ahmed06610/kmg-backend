using System.Security.Claims;
using KMG.Api.Authorization;
using KMG.Core.DTOs.Employee;
using KMG.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace KMG.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MisrInsuranceController : ControllerBase
    {
        private readonly IMisrInsuranceService _misrInsuranceService;

        public MisrInsuranceController(IMisrInsuranceService misrInsuranceService)
        {
            _misrInsuranceService = misrInsuranceService;
        }

        private int CurrentEmployeeId => int.Parse(User.FindFirstValue("EmployeeId") ?? "0");

        [HttpGet("summary")]
        [AuthorizeAbility("إدارة الرواتب")]
        public async Task<IActionResult> GetSummary()
        {
            return Ok(await _misrInsuranceService.GetEmployeeMisrInsuranceSummaryAsync());
        }

        [HttpGet("payouts")]
        [AuthorizeAbility("إدارة الرواتب")]
        public async Task<IActionResult> GetPayouts()
        {
            return Ok(await _misrInsuranceService.GetPayoutHistoryAsync());
        }

        [HttpPost("pay")]
        [AuthorizeAbility("إدارة الرواتب")]
        public async Task<IActionResult> Pay([FromBody] PayMisrInsuranceDTO model)
        {
            try
            {
                return Ok(await _misrInsuranceService.PayMisrInsuranceAsync(model, CurrentEmployeeId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
