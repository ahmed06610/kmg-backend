namespace KMG.Core.DTOs.AiContext
{
    // كل الحقول نصية عمدًا (حتى التواريخ) - دي بيانات واصلة من مصدر خارجي (خدمة الـ AI)
    // وتنسيقها مش مضمون يكون ثابت، فبنعرضها زي ما هي من غير ما نحاول نحولها لأنواع صارمة
    public class CreateAiTenderResultDTO
    {
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
    }

    public class AiTenderResultDTO : CreateAiTenderResultDTO
    {
        public int Id { get; set; }
        public DateTime FirstReceivedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }
    }

    // شكل الرد الفعلي من خدمة الـ AI الخارجية: مش مناقصة واحدة، دفعة كاملة من نتائج فحص واحد
    // (RunAt/TotalMatches بيانات وصفية عن الفحص نفسه - مش بتتخزن حاليًا، بس بنستقبلها عشان
    // الطلب ميفشلش، ولو احتجنا نتتبع تاريخ الفحصات لاحقًا هيبقوا جاهزين)
    public class IngestAiTenderResultsRequestDTO
    {
        public DateTime? RunAt { get; set; }
        public int? TotalMatches { get; set; }
        public List<CreateAiTenderResultDTO> Matches { get; set; } = new();
    }
}
