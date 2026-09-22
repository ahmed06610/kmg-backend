using KMG.Core.Enums;

namespace KMG.Core.Models
{
    public class SupplierPayment
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public decimal AmountCash { get; set; }
        public decimal AmountCredit { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? Notes { get; set; }

        // دفعة بشيك: بتتحسب فورًا كمديونية متسددة على المورد، لكن أثرها في الخزنة بيتأجل
        // لحد ما يتأكد إن الشيك اتصرف فعليًا (شوف SupplierService.ResolveCheckAsync)
        public bool IsCheck { get; set; }
        public DateTime? CheckDueDate { get; set; }
        public CheckStatus? CheckStatus { get; set; }

        public int SupplierId { get; set; }
        public virtual Supplier Supplier { get; set; } = null!;

        public int CreatedByEmployeeId { get; set; }
        public virtual Employee CreatedByEmployee { get; set; } = null!;
    }
}
