using System.Security.Claims;
using KMG.Api.Authorization;
using KMG.Core.DTOs.Payroll;
using KMG.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace KMG.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PayrollController : ControllerBase
    {
        private readonly IPayrollService _payrollService;

        public PayrollController(IPayrollService payrollService)
        {
            _payrollService = payrollService;
        }

        private int CurrentEmployeeId => int.Parse(User.FindFirstValue("EmployeeId") ?? "0");

        [HttpGet("advances")]
        [AuthorizeAbility("إدارة الرواتب")]
        public async Task<IActionResult> GetAdvances([FromQuery] int? employeeId)
        {
            return Ok(await _payrollService.GetAdvancesAsync(employeeId));
        }

        [HttpPost("advances")]
        [AuthorizeAbility("إدارة الرواتب")]
        public async Task<IActionResult> CreateAdvance([FromBody] CreateAdvanceDTO model)
        {
            return Ok(await _payrollService.CreateAdvanceAsync(model));
        }

        [HttpGet("adjustments")]
        [AuthorizeAbility("إدارة الرواتب")]
        public async Task<IActionResult> GetAdjustments([FromQuery] int? employeeId)
        {
            return Ok(await _payrollService.GetAdjustmentsAsync(employeeId));
        }

        [HttpPost("adjustments")]
        [AuthorizeAbility("إدارة الرواتب")]
        public async Task<IActionResult> CreateAdjustment([FromBody] CreateAdjustmentDTO model)
        {
            return Ok(await _payrollService.CreateAdjustmentAsync(model));
        }

        [HttpPost("preview")]
        [AuthorizeAbility("إدارة الرواتب")]
        public async Task<IActionResult> Preview([FromBody] RunPayrollDTO model)
        {
            try
            {
                return Ok(await _payrollService.PreviewPayrollAsync(model));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("run")]
        [AuthorizeAbility("إدارة الرواتب")]
        public async Task<IActionResult> Run([FromBody] RunPayrollDTO model)
        {
            try
            {
                return Ok(await _payrollService.RunPayrollAsync(model, CurrentEmployeeId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("history")]
        [AuthorizeAbility("إدارة الرواتب")]
        public async Task<IActionResult> GetHistory([FromQuery] int? employeeId)
        {
            return Ok(await _payrollService.GetPayoutHistoryAsync(employeeId));
        }
    }
}
