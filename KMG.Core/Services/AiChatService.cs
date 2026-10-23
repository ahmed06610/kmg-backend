using KMG.Core.DTOs.AiChat;
using KMG.Core.Helper;
using KMG.Core.Interfaces;
using KMG.Core.Interfaces.Services;
using KMG.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace KMG.Core.Services
{
    public class AiChatService : IAiChatService
    {
        public const int MaxPageSize = 50;
        public const int MaxMessageLength = 4000;

        // عدد الرسايل السابقة اللي بتتبعت مع كل رسالة جديدة كسياق للموديل - مش المحادثة كلها
        private const int HistoryTurns = 20;

        private readonly IUnitOfWork _unitOfWork;
        private readonly IAiChatClient _aiChatClient;

        public AiChatService(IUnitOfWork unitOfWork, IAiChatClient aiChatClient)
        {
            _unitOfWork = unitOfWork;
            _aiChatClient = aiChatClient;
        }

        public async Task<AiChatPageDTO> GetMessagesAsync(int employeeId, int? beforeId, int take)
        {
            take = Math.Clamp(take, 1, MaxPageSize);

            // بنجيب take + 1 تنازليًا: الزيادة دي بس عشان نعرف لو فيه رسايل أقدم (HasMore) من غير Count منفصل
            var page = await _unitOfWork.AiChatMessage
                .GetQueryable(m => m.EmployeeId == employeeId && (beforeId == null || m.Id < beforeId))
                .OrderByDescending(m => m.Id)
                .Take(take + 1)
                .AsNoTracking()
                .ToListAsync();

            var hasMore = page.Count > take;
            return new AiChatPageDTO
            {
                Items = page.Take(take).OrderBy(m => m.Id).Select(Map).ToList(),
                HasMore = hasMore
            };
        }

        public async Task<SendAiChatMessageResultDTO> SendAsync(int employeeId, string message, CancellationToken cancellationToken = default)
        {
            message = message?.Trim() ?? string.Empty;
            if (message.Length == 0)
                throw new Exception("اكتب رسالة الأول");
            if (message.Length > MaxMessageLength)
                throw new Exception($"الرسالة طويلة جدًا (الحد الأقصى {MaxMessageLength} حرف)");

            var employee = await _unitOfWork.Employee.GetByIdAsync(employeeId) ?? throw new Exception("المستخدم غير موجود");

            var history = await _unitOfWork.AiChatMessage
                .GetQueryable(m => m.EmployeeId == employeeId)
                .OrderByDescending(m => m.Id)
                .Take(HistoryTurns)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var prompt = (await _unitOfWork.AiPromptConfig.GetAllAsync()).FirstOrDefault()?.PromptText;

            // الطلب بيروح للـ AI الأول: لو فشل مفيش حاجة بتتحفظ، والمستخدم يقدر يعيد نفس الرسالة
            // من غير ما يفضل سؤال يتيم من غير رد في التاريخ
            var reply = await _aiChatClient.SendAsync(new AiChatRequestDTO
            {
                Message = message,
                History = history.OrderBy(m => m.Id).Select(m => new AiChatTurnDTO { Role = m.Role, Content = m.Content }).ToList(),
                SystemPrompt = string.IsNullOrWhiteSpace(prompt) ? null : prompt,
                UserName = employee.Name
            }, cancellationToken);

            if (string.IsNullOrWhiteSpace(reply))
                throw new Exception("خدمة الـ AI رجعت رد فاضي - جرّب تاني");

            var now = TimeHelper.NowInEgypt;
            var userMessage = new AiChatMessage { EmployeeId = employeeId, Role = AiChatMessage.UserRole, Content = message, CreatedAt = now };
            var assistantMessage = new AiChatMessage { EmployeeId = employeeId, Role = AiChatMessage.AssistantRole, Content = reply.Trim(), CreatedAt = TimeHelper.NowInEgypt };

            // الاتنين بيتضافوا بالترتيب ده عشان Id رسالة المستخدم يبقى أصغر من الرد بتاعها (الترتيب في العرض بالـ Id)،
            // وفي Transaction واحدة عشان ميتحفظش سؤال من غير رده
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _unitOfWork.AiChatMessage.AddAsync(userMessage);
                await _unitOfWork.CompleteAsync();
                await _unitOfWork.AiChatMessage.AddAsync(assistantMessage);
                await _unitOfWork.CompleteAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            return new SendAiChatMessageResultDTO { UserMessage = Map(userMessage), AssistantMessage = Map(assistantMessage) };
        }

        public async Task<int> ClearAsync(int employeeId)
        {
            return await _unitOfWork.AiChatMessage
                .GetQueryable(m => m.EmployeeId == employeeId)
                .ExecuteDeleteAsync();
        }

        private static AiChatMessageDTO Map(AiChatMessage m) => new()
        {
            Id = m.Id,
            Role = m.Role,
            Content = m.Content,
            CreatedAt = m.CreatedAt
        };
    }
}
