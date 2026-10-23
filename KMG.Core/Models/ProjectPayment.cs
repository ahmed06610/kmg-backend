using KMG.Core.Enums;

namespace KMG.Core.Models
{
    public class ProjectPayment
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public decimal AmountCash { get; set; }
        public decimal AmountCredit { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? Notes { get; set; }
        public string? AttachmentUrl { get; set; }
        public string? AttachmentFileName { get; set; }

        // دفعة بشيك: بتتحسب فورًا كتحصيل على المشروع، لكن أثرها في الخزنة بيتأجل لحد ما يتأكد
        // إن الشيك اتصرف فعليًا (شوف ProjectService.ResolveCheckAsync) - نفس فكرة SupplierPayment
        public bool IsCheck { get; set; }
        public DateTime? CheckDueDate { get; set; }
        public CheckStatus? CheckStatus { get; set; }

        public int ProjectId { get; set; }
        public virtual Project Project { get; set; } = null!;

        public int CreatedByEmployeeId { get; set; }
        public virtual Employee CreatedByEmployee { get; set; } = null!;
    }
}
