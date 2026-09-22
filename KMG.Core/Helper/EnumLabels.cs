using KMG.Core.Enums;

namespace KMG.Core.Helper
{
    // ترجمة القيم النصية للـ Enums للعربي - عشان أي نص حر (زي سجل التدقيق) يفضل عربي بالكامل
    // بدل ما يطلع فيه اسم الـ Enum بالإنجليزي زي "Manufacturing"
    public static class EnumLabels
    {
        private static readonly Dictionary<ProjectStatus, string> ProjectStatusLabels = new()
        {
            [ProjectStatus.New] = "جديد",
            [ProjectStatus.MaterialsReview] = "مراجعة الخامات",
            [ProjectStatus.PurchaseOrder] = "أمر شراء",
            [ProjectStatus.Manufacturing] = "تصنيع",
            [ProjectStatus.Mission] = "مأمورية",
            [ProjectStatus.Completed] = "مكتمل",
            [ProjectStatus.Closed] = "مغلق",
        };

        private static readonly Dictionary<ExpenseCategory, string> ExpenseCategoryLabels = new()
        {
            [ExpenseCategory.TenderInsurance] = "تأمين مناقصة",
            [ExpenseCategory.TenderTax] = "ضريبة مناقصة",
            [ExpenseCategory.Procedural] = "دفعة إجرائية",
            [ExpenseCategory.MissionSettlementDiff] = "فرق تسوية عهدة",
            [ExpenseCategory.Breakdown] = "عطل",
            [ExpenseCategory.Other] = "أخرى",
        };

        public static string Arabic(this ProjectStatus status) => ProjectStatusLabels.GetValueOrDefault(status, status.ToString());
        public static string Arabic(this ExpenseCategory category) => ExpenseCategoryLabels.GetValueOrDefault(category, category.ToString());
    }
}
