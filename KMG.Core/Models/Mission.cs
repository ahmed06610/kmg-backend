using System.ComponentModel.DataAnnotations.Schema;
using KMG.Core.Enums;

namespace KMG.Core.Models
{
    public class Mission
    {
        public int Id { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal AdvanceAmount { get; set; }   // العهدة المسلمة لرئيس العمال
        public decimal ActualSpent { get; set; }      // المصروف الفعلي بعد التسوية
        public MissionStatus Status { get; set; }
        public string? Notes { get; set; }

        public int ProjectId { get; set; }
        public virtual Project Project { get; set; } = null!;

        public int ForemanEmployeeId { get; set; }
        public virtual Employee ForemanEmployee { get; set; } = null!;

        public virtual ICollection<MissionWorker> MissionWorkers { get; set; } = new List<MissionWorker>();
        public virtual ICollection<CashBoxTransaction> CashBoxTransactions { get; set; } = new List<CashBoxTransaction>();

        // الفرق بين العهدة والمصروف الفعلي: موجب = فلوس راجعة للخزنة، سالب = مصروف نثري إضافي
        [NotMapped]
        public decimal SettlementDifference => AdvanceAmount - ActualSpent;

        // تكلفة عمالة المأمورية = مضاعف يومية كل عامل × عدد أيامه في المأمورية دي
        [NotMapped]
        public decimal TotalLaborCost => MissionWorkers.Sum(w => w.DaysCount * 2 * w.Employee.WageAmount);
    }
}
