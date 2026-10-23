using KMG.Core.DTOs.AiChat;

namespace KMG.Core.Interfaces.Services
{
    /// <summary>
    /// الاتصال الفعلي بخدمة الـ AI الخارجية - التنفيذ في طبقة الـ Api (HttpClient + الإعدادات)،
    /// عشان الـ Core يفضل مش معتمد على تفاصيل الشبكة.
    /// </summary>
    public interface IAiChatClient
    {
        /// <returns>نص رد الـ AI</returns>
        Task<string> SendAsync(AiChatRequestDTO request, CancellationToken cancellationToken = default);
    }
}
