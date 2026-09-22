namespace KMG.Core.Enums
{
    public enum TransactionType
    {
        ProjectPaymentIn = 1,      // تحصيل دفعة من عميل
        SupplierPaymentOut = 2,    // سداد لمورد
        ProjectExpenseOut = 3,     // مصروف نثري لمشروع
        MissionAdvanceOut = 4,     // تسليم عهدة مأمورية
        MissionSettlementIn = 5,   // استرجاع فارق عهدة (لو المصروف أقل من العهدة)
        PayrollOut = 6,            // صرف راتب/أجر
        StockPurchaseOut = 7,      // دفع نقدي وقت شراء خامة (اختياري لو الشراء كاش وقتها)
        AdvanceOut = 8,            // تسليم سلفة لموظف
        MissionSettlementOut = 9,  // سداد فارق عهدة (لو المصروف أكتر من العهدة)
        MiscExpenseOut = 10        // مصروف نثري عام (مش مرتبط بمشروع معين)
    }

    public enum MiscExpenseCategory
    {
        Administrative = 1, // إداري
        Operational = 2,    // تشغيلي
        Other = 3           // أخرى
    }
}
