using System.Security.Claims;
using KMG.Api.Authorization;
using KMG.Core.DTOs.Employee;
using KMG.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace KMG.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InsuranceController : ControllerBase
    {
        private readonly IInsuranceService _insuranceService;

        public InsuranceController(IInsuranceService insuranceService)
        {
            _insuranceService = insuranceService;
        }

        private int CurrentEmployeeId => int.Parse(User.FindFirstValue("EmployeeId") ?? "0");

        [HttpGet("summary")]
        [AuthorizeAbility("إدارة الرواتب")]
        public async Task<IActionResult> GetSummary()
        {
            return Ok(await _insuranceService.GetEmployeeInsuranceSummaryAsync());
        }

        [HttpGet("payouts")]
        [AuthorizeAbility("إدارة الرواتب")]
        public async Task<IActionResult> GetPayouts()
        {
            return Ok(await _insuranceService.GetPayoutHistoryAsync());
        }

        [HttpPost("pay")]
        [AuthorizeAbility("إدارة الرواتب")]
        public async Task<IActionResult> Pay([FromBody] PayInsuranceDTO model)
        {
            try
            {
                return Ok(await _insuranceService.PayInsuranceAsync(model, CurrentEmployeeId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
