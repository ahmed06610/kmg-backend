using KMG.Core.Enums;

namespace KMG.Core.Models
{
    public class Advance
    {
        public int Id { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal InstallmentAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public DateTime IssueDate { get; set; }
        public AdvanceStatus Status { get; set; }
        public string? Notes { get; set; }

        public int EmployeeId { get; set; }
        public virtual Employee Employee { get; set; } = null!;

        public virtual ICollection<CashBoxTransaction> CashBoxTransactions { get; set; } = new List<CashBoxTransaction>();
    }
}
