namespace KMG.Core.DTOs.AiChat
{
    public class AiChatMessageDTO
    {
        public int Id { get; set; }
        /// <summary>"user" أو "assistant"</summary>
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>دفعة رسايل مرتبة من الأقدم للأحدث + هل فيه رسايل أقدم لسه متحملتش</summary>
    public class AiChatPageDTO
    {
        public List<AiChatMessageDTO> Items { get; set; } = new();
        public bool HasMore { get; set; }
    }

    public class SendAiChatMessageDTO
    {
        public string Message { get; set; } = string.Empty;
    }

    public class SendAiChatMessageResultDTO
    {
        public AiChatMessageDTO UserMessage { get; set; } = null!;
        public AiChatMessageDTO AssistantMessage { get; set; } = null!;
    }

    /// <summary>رسالة بالشكل اللي بيتبعت لخدمة الـ AI: { role, content }</summary>
    public class AiChatTurnDTO
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>الطلب اللي بيتبعت لخدمة الـ AI الخارجية</summary>
    public class AiChatRequestDTO
    {
        /// <summary>رسالة المستخدم الجديدة</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>آخر رسايل المحادثة قبل الرسالة الجديدة (من الأقدم للأحدث) عشان الموديل يفهم السياق</summary>
        public List<AiChatTurnDTO> History { get; set; } = new();

        /// <summary>البرومبت الديناميكي اللي صاحب العمل كاتبه في صفحة "تكامل AI"</summary>
        public string? SystemPrompt { get; set; }

        public string UserName { get; set; } = string.Empty;
    }
}
