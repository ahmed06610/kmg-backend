using KMG.Core.Enums;

namespace KMG.Core.Models
{
    public class ProjectExpense
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public ExpenseCategory Category { get; set; }
        public string? Description { get; set; }
        public DateTime ExpenseDate { get; set; }
        public string? AttachmentUrl { get; set; }

        public int ProjectId { get; set; }
        public virtual Project Project { get; set; } = null!;

        public int CreatedByEmployeeId { get; set; }
        public virtual Employee CreatedByEmployee { get; set; } = null!;

        public virtual ICollection<CashBoxTransaction> CashBoxTransactions { get; set; } = new List<CashBoxTransaction>();
    }
}
