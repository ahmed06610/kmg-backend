using System.Security.Claims;
using KMG.Api.Authorization;
using KMG.Core.DTOs.AiContext;
using KMG.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace KMG.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AiContextController : ControllerBase
    {
        private readonly IAiContextService _aiContextService;

        public AiContextController(IAiContextService aiContextService)
        {
            _aiContextService = aiContextService;
        }

        private int CurrentEmployeeId => int.Parse(User.FindFirstValue("EmployeeId") ?? "0");

        // بينادى من خدمة الـ AI الخارجية بمفتاح API ثابت - مش من موظف مسجل دخول
        [HttpGet("data")]
        [ServiceFilter(typeof(ApiKeyAuthFilter))]
        public async Task<IActionResult> GetData()
        {
            return Ok(await _aiContextService.GetContextDataAsync());
        }

        [HttpGet("prompt")]
        [AuthorizeAbility("إدارة تكامل AI")]
        public async Task<IActionResult> GetPrompt()
        {
            return Ok(await _aiContextService.GetPromptAsync());
        }

        [HttpPut("prompt")]
        [AuthorizeAbility("إدارة تكامل AI")]
        public async Task<IActionResult> UpdatePrompt([FromBody] UpdateAiPromptDTO model)
        {
            try
            {
                return Ok(await _aiContextService.UpdatePromptAsync(model.PromptText, CurrentEmployeeId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // بينادى من خدمة الـ AI الخارجية بمفتاح API ثابت - بتبعت دفعة كاملة من نتائج فحص واحد
        // (مش مناقصة واحدة - الرد بيوصل كـ { runAt, totalMatches, matches: [...] })
        [HttpPost("aiResults")]
        [ServiceFilter(typeof(ApiKeyAuthFilter))]
        public async Task<IActionResult> IngestTenderResults([FromBody] IngestAiTenderResultsRequestDTO model)
        {
            try
            {
                return Ok(await _aiContextService.IngestTenderResultsAsync(model.Matches));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("aiResults")]
        [AuthorizeAbility("إدارة تكامل AI")]
        public async Task<IActionResult> GetTenderResults()
        {
            return Ok(await _aiContextService.GetActiveTenderResultsAsync());
        }

        [HttpGet("aiResults/{id}")]
        [AuthorizeAbility("إدارة تكامل AI")]
        public async Task<IActionResult> GetTenderResult(int id)
        {
            var result = await _aiContextService.GetTenderResultByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost("aiResults/{id}/dismiss")]
        [AuthorizeAbility("إدارة تكامل AI")]
        public async Task<IActionResult> DismissTenderResult(int id)
        {
            await _aiContextService.DismissTenderResultAsync(id);
            return Ok();
        }
    }
}
