namespace KMG.Core.Enums
{
    public enum MovementType
    {
        Purchase = 1,           // شراء
        IssueToProject = 2,     // صرف لمشروع
        ReturnFromProject = 3,  // مرتجع من مشروع
        OpeningBalance = 4,     // رصيد افتتاحي عند إنشاء الخامة
        Adjustment = 5          // تسوية جرد (الكمية بالإشارة: موجب = زيادة، سالب = عجز)
    }
}
