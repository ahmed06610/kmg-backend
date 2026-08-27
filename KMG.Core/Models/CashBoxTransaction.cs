using KMG.Core.Enums;
using KMG.Core.Helper;

namespace KMG.Core.Models
{
    public class CashBoxTransaction
    {
        public int Id { get; set; }
        public decimal AmountCash { get; set; }
        public decimal AmountCredit { get; set; }
        public TransactionType TransactionType { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; } = TimeHelper.NowInEgypt;

        public int CashBoxId { get; set; }
        public virtual CashBox CashBox { get; set; } = null!;

        // مصدر الحركة - Nullable FKs عشان كل حركة يبقى معروف منين جاية
        public int? ProjectId { get; set; }
        public virtual Project? Project { get; set; }

        public int? SupplierId { get; set; }
        public virtual Supplier? Supplier { get; set; }

        public int? ProjectExpenseId { get; set; }
        public virtual ProjectExpense? ProjectExpense { get; set; }

        public int? MissionId { get; set; }
        public virtual Mission? Mission { get; set; }

        public int? PayrollPayoutId { get; set; }
        public virtual PayrollPayout? PayrollPayout { get; set; }

        public int CreatedByEmployeeId { get; set; }
        public virtual Employee CreatedByEmployee { get; set; } = null!;
    }
}
