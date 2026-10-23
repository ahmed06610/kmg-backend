namespace KMG.Core.Models
{
    // سجل بكل قسط سلفة اتخصم فعليًا جوه راتب معين - عشان لو الراتب ده اتحذف نقدر نرجّع
    // المبلغ المظبوط لرصيد السلفة من غير ما نحتاج نخمّن قد إيه كان مخصوم منها
    public class PayrollAdvanceCharge
    {
        public int Id { get; set; }
        public decimal ChargedAmount { get; set; }

        public int PayrollPayoutId { get; set; }
        public virtual PayrollPayout PayrollPayout { get; set; } = null!;

        public int AdvanceId { get; set; }
        public virtual Advance Advance { get; set; } = null!;
    }
}
