using KMG.Core.DTOs.AiChat;

namespace KMG.Core.Interfaces.Services
{
    public interface IAiChatService
    {
        /// <param name="beforeId">لو null بيرجع أحدث دفعة، وإلا الدفعة اللي قبل الرسالة دي</param>
        Task<AiChatPageDTO> GetMessagesAsync(int employeeId, int? beforeId, int take);

        Task<SendAiChatMessageResultDTO> SendAsync(int employeeId, string message, CancellationToken cancellationToken = default);

        Task<int> ClearAsync(int employeeId);
    }
}
