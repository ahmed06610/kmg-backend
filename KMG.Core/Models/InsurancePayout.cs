namespace KMG.Core.Models
{
    /// <summary>
    /// دفعة صرف تأمينات الموظفين - مبلغ إجمالي واحد يغطي فترة معينة (عادةً شهر)،
    /// بيتحسب وقت الصرف من مجموع InsuranceAmount لكل الموظفين النشطين.
    /// </summary>
    public class InsurancePayout
    {
        public int Id { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime PaidDate { get; set; }
        public string? Notes { get; set; }

        public int CreatedByEmployeeId { get; set; }
        public virtual Employee CreatedByEmployee { get; set; } = null!;

        public virtual ICollection<CashBoxTransaction> CashBoxTransactions { get; set; } = new List<CashBoxTransaction>();
    }
}
