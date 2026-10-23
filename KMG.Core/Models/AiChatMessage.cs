namespace KMG.Core.Models
{
    /// <summary>
    /// رسالة واحدة في محادثة صاحب العمل مع الـ AI. بتتخزن بنفس شكل رسائل نماذج الـ AI
    /// (Role + Content) عشان التاريخ يتبعت للموديل زي ما هو من غير أي تحويل.
    /// </summary>
    public class AiChatMessage
    {
        public const string UserRole = "user";
        public const string AssistantRole = "assistant";

        public int Id { get; set; }

        /// <summary>"user" أو "assistant"</summary>
        public string Role { get; set; } = UserRole;

        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        // المحادثة خاصة بكل مستخدم - كل واحد بيشوف رسايله هو بس
        public int EmployeeId { get; set; }
        public virtual Employee Employee { get; set; } = null!;
    }
}
