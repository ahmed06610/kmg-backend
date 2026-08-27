namespace KMG.Core.Enums
{
    public enum ProjectType
    {
        ManufactureExecution = 1, // تصنيع وتنفيذ (مناقصات شركات)
        Subcontracting = 2,       // مقاولات من الباطن (تعامل مباشر)
        Supply = 3                // توريد خامات
    }

    public enum ProjectStatus
    {
        New = 1,
        MaterialsReview = 2,
        PurchaseOrder = 3,
        Manufacturing = 4,
        Mission = 5,
        Completed = 6,
        Closed = 7
    }

    public enum ExpenseCategory
    {
        TenderInsurance = 1,      // تأمين المناقصة
        TenderTax = 2,             // ضريبة المناقصة
        Procedural = 3,            // دفعات إجرائية
        MissionSettlementDiff = 4, // فرق تسوية عهدة مأمورية
        Breakdown = 5,             // عطل
        Other = 6
    }
}
