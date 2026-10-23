using System.Security.Claims;
using KMG.Api.Authorization;
using KMG.Core.DTOs.AiChat;
using KMG.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace KMG.Api.Controllers
{
    // محادثة صاحب العمل مع الـ AI - كل مستخدم بيشوف ويبعت في محادثته هو بس
    [Route("api/[controller]")]
    [ApiController]
    public class AiChatController : ControllerBase
    {
        private readonly IAiChatService _aiChatService;

        public AiChatController(IAiChatService aiChatService)
        {
            _aiChatService = aiChatService;
        }

        private int CurrentEmployeeId => int.Parse(User.FindFirstValue("EmployeeId") ?? "0");

        /// <summary>
        /// بيرجع دفعة رسايل (من الأقدم للأحدث). من غير beforeId = أحدث دفعة،
        /// ومع beforeId = الدفعة اللي قبل الرسالة دي (للتحميل لما المستخدم يسكرول لفوق).
        /// </summary>
        [HttpGet("messages")]
        [AuthorizeAbility("إدارة تكامل AI")]
        public async Task<IActionResult> GetMessages([FromQuery] int? beforeId, [FromQuery] int take = 20)
        {
            return Ok(await _aiChatService.GetMessagesAsync(CurrentEmployeeId, beforeId, take));
        }

        /// <summary>بيبعت الرسالة للـ AI، ولما الرد يوصل بيحفظ الرسالة والرد مع بعض ويرجّعهم</summary>
        [HttpPost("send")]
        [AuthorizeAbility("إدارة تكامل AI")]
        public async Task<IActionResult> Send([FromBody] SendAiChatMessageDTO model, CancellationToken cancellationToken)
        {
            try
            {
                return Ok(await _aiChatService.SendAsync(CurrentEmployeeId, model.Message, cancellationToken));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("messages")]
        [AuthorizeAbility("إدارة تكامل AI")]
        public async Task<IActionResult> Clear()
        {
            var deleted = await _aiChatService.ClearAsync(CurrentEmployeeId);
            return Ok(new { deleted });
        }
    }
}
