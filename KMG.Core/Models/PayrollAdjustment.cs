using KMG.Core.Enums;

namespace KMG.Core.Models
{
    public class PayrollAdjustment
    {
        public int Id { get; set; }
        public AdjustmentType Type { get; set; }
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public bool Applied { get; set; } // اتخصم/اتضاف بالفعل في PayrollPayout ولا لسه

        public int EmployeeId { get; set; }
        public virtual Employee Employee { get; set; } = null!;
    }
}
