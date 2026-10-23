using System.Security.Claims;
using KMG.Api.Authorization;
using KMG.Core.DTOs.CashBox;
using KMG.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace KMG.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustodyController : ControllerBase
    {
        private readonly ICustodyService _custodyService;

        public CustodyController(ICustodyService custodyService)
        {
            _custodyService = custodyService;
        }

        private int CurrentEmployeeId => int.Parse(User.FindFirstValue("EmployeeId") ?? "0");

        [HttpGet]
        [AuthorizeAbility("عرض الخزنة")]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _custodyService.GetAllAsync());
        }

        [HttpPost]
        [AuthorizeAbility("إدارة المصاريف")]
        public async Task<IActionResult> Create([FromBody] CreateCustodyDTO model)
        {
            try
            {
                return Ok(await _custodyService.CreateAsync(model, CurrentEmployeeId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut]
        [AuthorizeAbility("إدارة المصاريف")]
        public async Task<IActionResult> Update([FromBody] UpdateCustodyDTO model)
        {
            try
            {
                return Ok(await _custodyService.UpdateAsync(model, CurrentEmployeeId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        [AuthorizeAbility("إدارة المصاريف")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _custodyService.DeleteAsync(id, CurrentEmployeeId);
                return result ? Ok() : NotFound();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("settle")]
        [AuthorizeAbility("إدارة المصاريف")]
        public async Task<IActionResult> Settle([FromBody] SettleCustodyDTO model)
        {
            try
            {
                return Ok(await _custodyService.SettleAsync(model, CurrentEmployeeId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
