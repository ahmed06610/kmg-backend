using KMG.Core.Enums;

namespace KMG.Core.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public NotificationType Type { get; set; }
        public NotificationSeverity Severity { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        // مفتاح ثابت لكل "حالة" منطقية (زي "LowStock:14" أو "ProjectPaymentFollowUp:3:202609")
        // بيستخدم عشان نعرف نحدّث/نمسح نفس التنبيه بدل ما نكرره كل مرة بيتعمل فيها Sync
        public string DedupeKey { get; set; } = string.Empty;

        public string? LinkUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public bool IsDismissed { get; set; }
    }
}
