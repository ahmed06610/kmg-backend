namespace KMG.Core.Models
{
    public class GeneratedInvoiceLineItem
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        public int GeneratedInvoiceId { get; set; }
        public virtual GeneratedInvoice GeneratedInvoice { get; set; } = null!;
    }
}
