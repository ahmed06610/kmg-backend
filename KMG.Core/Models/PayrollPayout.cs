namespace KMG.Core.Models
{
    public class PayrollPayout
    {
        public int Id { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public decimal BaseAmount { get; set; }
        public decimal MissionDoubleUpAmount { get; set; }
        public decimal DeductionsAmount { get; set; }
        public decimal BonusAmount { get; set; }
        public decimal AdvanceInstallmentAmount { get; set; }
        public decimal InsuranceAmount { get; set; }
        public decimal MisrInsuranceAmount { get; set; }
        public decimal NetPaid { get; set; }
        public DateTime PaidDate { get; set; }

        // تفاصيل إضافية للعرض/التصدير بس (مش جزء من حساب الصافي - المبالغ دي بالفعل جوه BaseAmount/MissionDoubleUpAmount/DeductionsAmount/BonusAmount)
        public decimal WorkshopDays { get; set; } // أيام ورشة (الفترة العادية من غير مأمورية)
        public decimal MissionDays { get; set; } // أيام سفر/مأمورية داخل الفترة (ممكن تبقى كسر)
        public decimal HourlyDeductionAmount { get; set; } // جزء من DeductionsAmount محسوب بالساعة
        public decimal HourlyBonusAmount { get; set; } // جزء من BonusAmount محسوب بالساعة

        public int EmployeeId { get; set; }
        public virtual Employee Employee { get; set; } = null!;

        public int CreatedByEmployeeId { get; set; }
        public virtual Employee CreatedByEmployee { get; set; } = null!;

        public virtual ICollection<CashBoxTransaction> CashBoxTransactions { get; set; } = new List<CashBoxTransaction>();
        public virtual ICollection<PayrollAdvanceCharge> AdvanceCharges { get; set; } = new List<PayrollAdvanceCharge>();
    }
}
