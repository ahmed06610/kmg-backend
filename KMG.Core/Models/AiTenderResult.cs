namespace KMG.Core.Models
{
    // نتيجة مطابقة مناقصة جاية من خدمة الـ AI الخارجية (زميل الـ AI) - كل الحقول نصية عمدًا
    // (حتى التواريخ) لأنها بتوصف "كما هي" من مصدر خارجي ومش هنعتمد على تنسيقها في أي حسابات
    public class AiTenderResult
    {
        public int Id { get; set; }

        // المفتاح الخارجي الثابت للمناقصة - بيستخدم لتحديث نفس السجل بدل تكراره كل مرة يوصل تحديث
        public string TenderId { get; set; } = string.Empty;

        public string? TenderTitle { get; set; }
        public string? IssuingEntity { get; set; }
        public string? SourceSite { get; set; }
        public string? SourceUrl { get; set; }
        public string? SubmissionDeadline { get; set; }
        public int? DaysUntilDeadline { get; set; }
        public string? BusinessCategory { get; set; }
        public bool? IsNewCategory { get; set; }
        public string? MatchedVia { get; set; }
        public string? MatchReason { get; set; }

        // بيانات مستخرجة من كراسة الشروط (اختيارية - بتوصل بس لو الـ AI قدر يقرأ ملف/صفحة المناقصة)
        public string? DocumentReadStatus { get; set; }
        public string? DocumentEntity { get; set; }
        public string? ScopeOfWork { get; set; }
        public string? MaterialsRequired { get; set; }
        public string? Quantities { get; set; }
        public string? Location { get; set; }
        public string? BookletFee { get; set; }
        public string? InitialInsurance { get; set; }
        public string? DocumentSubmitBy { get; set; }
        public string? ContactInfo { get; set; }
        public bool? AiDocumentRelevant { get; set; }
        public string? RelevanceNote { get; set; }

        public DateTime FirstReceivedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }

        // "مش مهتم" - بيختفي من القائمة المعروضة من غير ما يتمسح فعليًا
        public bool IsDismissed { get; set; }
    }
}
