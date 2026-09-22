namespace KMG.Core.Models
{
    // صف واحد فقط للشركة كلها (Singleton) - البرومبت الديناميكي المستخدم في تكامل الـ AI الخارجي
    public class AiPromptConfig
    {
        public int Id { get; set; }
        public string PromptText { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }

        public int? UpdatedByEmployeeId { get; set; }
        public virtual Employee? UpdatedByEmployee { get; set; }
    }
}
