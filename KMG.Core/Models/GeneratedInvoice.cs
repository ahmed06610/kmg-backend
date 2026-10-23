namespace KMG.Core.Models
{
    public class GeneratedInvoice
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string? Title { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime? DueDate { get; set; }

        public string RecipientName { get; set; } = string.Empty;
        public string? RecipientAddress { get; set; }
        public string? RecipientPhone { get; set; }

        public int? ProjectId { get; set; }
        public virtual Project? Project { get; set; }

        public string? Notes { get; set; }
        public decimal? TaxPercent { get; set; }

        // اسم منشئ الفاتورة اللي بيظهر عليها بيتكتب يدويًا ومستقل عن اسم الموظف المسجل دخوله فعليًا،
        // وظهوره وظهور خانة التوقيع اختياريين بالكامل (المستخدم بيتحكم فيهم وقت الإنشاء)
        public string? CreatorDisplayName { get; set; }
        public bool ShowCreatorName { get; set; }
        public bool ShowSignature { get; set; }

        public int CreatedByEmployeeId { get; set; }
        public virtual Employee CreatedByEmployee { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        public virtual ICollection<GeneratedInvoiceLineItem> LineItems { get; set; } = new List<GeneratedInvoiceLineItem>();
    }
}
