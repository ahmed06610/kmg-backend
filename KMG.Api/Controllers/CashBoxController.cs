using System.Security.Claims;
using KMG.Api.Authorization;
using KMG.Core.DTOs.CashBox;
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

        private int CurrentEmployeeId => int.Parse(User.FindFirstValue("EmployeeId") ?? "0");

        [HttpGet]
        [AuthorizeAbility("عرض الخزنة")]
        public async Task<IActionResult> GetDetails([FromQuery] int recentCount = 50)
        {
            return Ok(await _cashBoxService.GetDetailsAsync(recentCount));
        }

        [HttpGet("transactions")]
        [AuthorizeAbility("عرض الخزنة")]
        public async Task<IActionResult> GetTransactions([FromQuery] CashBoxTransactionFilterDTO filter)
        {
            return Ok(await _cashBoxService.GetTransactionsAsync(filter));
        }

        [HttpPost("misc-expenses")]
        [AuthorizeAbility("إدارة المصاريف")]
        public async Task<IActionResult> CreateMiscExpense([FromBody] CreateMiscExpenseDTO model)
        {
            try
            {
                return Ok(await _cashBoxService.CreateMiscExpenseAsync(model, CurrentEmployeeId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("misc-expenses")]
        [AuthorizeAbility("إدارة المصاريف")]
        public async Task<IActionResult> UpdateMiscExpense([FromBody] UpdateMiscExpenseDTO model)
        {
            try
            {
                return Ok(await _cashBoxService.UpdateMiscExpenseAsync(model, CurrentEmployeeId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("misc-expenses/{id}")]
        [AuthorizeAbility("إدارة المصاريف")]
        public async Task<IActionResult> DeleteMiscExpense(int id)
        {
            try
            {
                var result = await _cashBoxService.DeleteMiscExpenseAsync(id);
                return result ? Ok() : NotFound();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
