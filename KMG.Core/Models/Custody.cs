using System.ComponentModel.DataAnnotations.Schema;
using KMG.Core.Enums;

namespace KMG.Core.Models
{
    /// <summary>
    /// عهدة جانبية بتتصرف لموظف مباشرة - مش مرتبطة بمأمورية معينة (زي Mission.AdvanceAmount)
    /// ولا سلفة شخصية بتتخصم من المرتب (زي Advance) - ممكن تتربط بمشروع اختياريًا وتحتاج تسوية.
    /// </summary>
    public class Custody
    {
        public int Id { get; set; }
        public decimal AmountCash { get; set; }
        public decimal AmountCredit { get; set; }

        [NotMapped]
        public decimal Amount => AmountCash + AmountCredit;

        public string Description { get; set; } = string.Empty;
        public DateTime IssueDate { get; set; }
        public CustodyStatus Status { get; set; }
        public decimal? SettledAmount { get; set; }
        public DateTime? SettledDate { get; set; }
        public string? Notes { get; set; }

        public int EmployeeId { get; set; }
        public virtual Employee Employee { get; set; } = null!;

        public int? ProjectId { get; set; }
        public virtual Project? Project { get; set; }

        public virtual ICollection<CashBoxTransaction> CashBoxTransactions { get; set; } = new List<CashBoxTransaction>();
    }
}
