using KMG.Core.Enums;

namespace KMG.Core.Models
{
    public class PayrollAdjustment
    {
        public int Id { get; set; }
        public AdjustmentType Type { get; set; }
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public bool Applied { get; set; } // اتخصم/اتضاف بالفعل في PayrollPayout ولا لسه

        // لو القيمة اتحسبت بالساعة بدل ما تتكتب مباشرة - بنسجلهم هنا للشفافية بس
        // (Amount هو المصدر الوحيد اللي بيتحسب بيه الراتب فعليًا، الحقلين دول للعرض والتعديل لاحقًا بس)
        public decimal? Hours { get; set; }
        public decimal? HourlyRate { get; set; }

        public int EmployeeId { get; set; }
        public virtual Employee Employee { get; set; } = null!;

        // لو اتطبقت، مرتبطة بأي PayrollPayout بالظبط - عشان لو الراتب ده اتحذف نقدر نرجّع
        // التسوية دي "غير مطبقة" تاني بدل ما تفضل محسوبة Applied من غير أي راتب بيستخدمها
        public int? AppliedInPayrollPayoutId { get; set; }
        public virtual PayrollPayout? AppliedInPayrollPayout { get; set; }
    }
}
