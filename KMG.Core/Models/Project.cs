using System.ComponentModel.DataAnnotations.Schema;
using KMG.Core.Enums;

namespace KMG.Core.Models
{
    public class Project
    {
        public int Id { get; set; }
        public string ProjectCode { get; set; } = string.Empty;
        public ProjectType ProjectType { get; set; }
        public ProjectStatus Status { get; set; }
        public string? Description { get; set; }

        public decimal ContractValue { get; set; }

        // خاص بمشاريع المناقصات (تصنيع وتنفيذ)
        public decimal? TenderInsuranceAmount { get; set; }
        public decimal? TenderTaxAmount { get; set; }

        // خاص بمشاريع التوريد
        public decimal? SupplyProfitMargin { get; set; }

        public DateTime CreatedAt { get; set; }

        public int ClientId { get; set; }
        public virtual Client Client { get; set; } = null!;

        public int CreatedByEmployeeId { get; set; }
        public virtual Employee CreatedByEmployee { get; set; } = null!;

        public virtual ICollection<ProjectPayment> Payments { get; set; } = new List<ProjectPayment>();
        public virtual ICollection<ProjectExpense> Expenses { get; set; } = new List<ProjectExpense>();
        public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
        public virtual ICollection<Mission> Missions { get; set; } = new List<Mission>();
        public virtual ICollection<ProjectAttachment> Attachments { get; set; } = new List<ProjectAttachment>();
        public virtual ICollection<ProjectAudit> AuditLogs { get; set; } = new List<ProjectAudit>();
        public virtual ICollection<CashBoxTransaction> CashBoxTransactions { get; set; } = new List<CashBoxTransaction>();

        // تكلفة الخامات = (صرف للمشروع - مرتجع من المشروع) بسعر وقت الحركة
        [NotMapped]
        public decimal TotalMaterialsCost =>
            StockMovements.Where(m => m.MovementType == MovementType.IssueToProject).Sum(m => m.Quantity * m.UnitPriceAtTime)
            - StockMovements.Where(m => m.MovementType == MovementType.ReturnFromProject).Sum(m => m.Quantity * m.UnitPriceAtTime);

        [NotMapped]
        public decimal TotalPettyExpenses => Expenses.Sum(e => e.Amount);

        [NotMapped]
        public decimal TotalLaborCost => Missions.Sum(m => m.TotalLaborCost);

        [NotMapped]
        public decimal TotalCollected => Payments.Sum(p => p.Amount);

        [NotMapped]
        public decimal RemainingBalance => ContractValue - TotalCollected;

        [NotMapped]
        public decimal NetProfit => ContractValue - (TotalMaterialsCost + TotalPettyExpenses + TotalLaborCost);
    }
}
