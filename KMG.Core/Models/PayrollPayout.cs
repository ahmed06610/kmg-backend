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
        public decimal NetPaid { get; set; }
        public DateTime PaidDate { get; set; }

        public int EmployeeId { get; set; }
        public virtual Employee Employee { get; set; } = null!;

        public int CreatedByEmployeeId { get; set; }
        public virtual Employee CreatedByEmployee { get; set; } = null!;

        public virtual ICollection<CashBoxTransaction> CashBoxTransactions { get; set; } = new List<CashBoxTransaction>();
    }
}
