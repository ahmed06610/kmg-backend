using System.Security.Claims;
using KMG.Api.Authorization;
using KMG.Core.DTOs.Stock;
using KMG.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace KMG.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StockController : ControllerBase
    {
        private readonly IStockService _stockService;

        public StockController(IStockService stockService)
        {
            _stockService = stockService;
        }

        private int CurrentEmployeeId => int.Parse(User.FindFirstValue("EmployeeId") ?? "0");

        [HttpGet("materials")]
        [AuthorizeAbility("عرض المخزن")]
        public async Task<IActionResult> GetAllMaterials()
        {
            return Ok(await _stockService.GetAllMaterialsAsync());
        }

        [HttpGet("materials/{id}")]
        [AuthorizeAbility("عرض المخزن")]
        public async Task<IActionResult> GetMaterialById(int id)
        {
            var material = await _stockService.GetMaterialByIdAsync(id);
            return material == null ? NotFound() : Ok(material);
        }

        [HttpPost("materials")]
        [AuthorizeAbility("إدارة المخزن")]
        public async Task<IActionResult> CreateMaterial([FromBody] CreateMaterialDTO model)
        {
            var id = await _stockService.CreateMaterialAsync(model, CurrentEmployeeId);
            return Ok(id);
        }

        [HttpPut("materials")]
        [AuthorizeAbility("إدارة المخزن")]
        public async Task<IActionResult> UpdateMaterial([FromBody] UpdateMaterialDTO model)
        {
            var result = await _stockService.UpdateMaterialAsync(model);
            return result ? Ok() : NotFound();
        }

        [HttpGet("movements")]
        [AuthorizeAbility("عرض المخزن")]
        public async Task<IActionResult> GetMovements([FromQuery] int? materialId, [FromQuery] int? projectId)
        {
            return Ok(await _stockService.GetMovementsAsync(materialId, projectId));
        }

        [HttpPost("purchase")]
        [AuthorizeAbility("إدارة المخزن")]
        public async Task<IActionResult> RecordPurchase([FromBody] CreatePurchaseDTO model)
        {
            try
            {
                return Ok(await _stockService.RecordPurchaseAsync(model, CurrentEmployeeId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("issue")]
        [AuthorizeAbility("إدارة المخزن")]
        public async Task<IActionResult> IssueToProject([FromBody] CreateIssueDTO model)
        {
            try
            {
                return Ok(await _stockService.IssueToProjectAsync(model, CurrentEmployeeId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("return")]
        [AuthorizeAbility("إدارة المخزن")]
        public async Task<IActionResult> ReturnFromProject([FromBody] CreateReturnDTO model)
        {
            try
            {
                return Ok(await _stockService.ReturnFromProjectAsync(model, CurrentEmployeeId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
