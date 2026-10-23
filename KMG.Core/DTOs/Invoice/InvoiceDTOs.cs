namespace KMG.Core.DTOs.Invoice
{
    /// <summary>
    /// شكل موحّد لعرض مرفق فاتورة/إيصال، سواء كان مصدره دفعة مشروع أو مصروف مشروع
    /// أو شراء/صرف مخزون أو دفعة مورد أو مصروف نثري - كلهم بيتحوّلوا لنفس الشكل ده للعرض فقط.
    /// </summary>
    public class InvoiceDTO
    {
        public string SourceType { get; set; } = string.Empty; // ProjectPayment, ProjectExpense, StockPurchase, StockIssue, SupplierPayment, MiscExpense
        public int SourceId { get; set; }
        public string Direction { get; set; } = string.Empty; // In, Out
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string? AttachmentUrl { get; set; }
        public string? AttachmentFileName { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Notes { get; set; }

        public int? ProjectId { get; set; }
        public string? ProjectName { get; set; }
        public int? SupplierId { get; set; }
        public string? SupplierName { get; set; }

        public string CreatedByEmployeeName { get; set; } = string.Empty;
    }

    public class InvoiceFilterDTO
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SourceType { get; set; }
        public string? Direction { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int? ProjectId { get; set; }
        public int? SupplierId { get; set; }
        public string? Search { get; set; }
    }

    public class PagedResultDTO<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class GeneratedInvoiceLineItemDTO
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Total => Quantity * UnitPrice;
    }

    public class GeneratedInvoiceListDTO
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string? Title { get; set; }
        public DateTime IssueDate { get; set; }
        public string RecipientName { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string? ProjectName { get; set; }
        public bool ShowSignature { get; set; }
    }

    public class GeneratedInvoiceDTO
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
        public string? ProjectName { get; set; }

        public string? Notes { get; set; }
        public decimal? TaxPercent { get; set; }

        public string? CreatorDisplayName { get; set; }
        public bool ShowCreatorName { get; set; }
        public bool ShowSignature { get; set; }

        public string CreatedByEmployeeName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public List<GeneratedInvoiceLineItemDTO> LineItems { get; set; } = new();

        public decimal Subtotal => LineItems.Sum(li => li.Total);
        public decimal TaxAmount => TaxPercent.HasValue ? Subtotal * (TaxPercent.Value / 100m) : 0;
        public decimal Total => Subtotal + TaxAmount;
    }

    public class CreateGeneratedInvoiceLineItemDTO
    {
        public string Description { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class CreateGeneratedInvoiceDTO
    {
        public string? Title { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime? DueDate { get; set; }

        public string RecipientName { get; set; } = string.Empty;
        public string? RecipientAddress { get; set; }
        public string? RecipientPhone { get; set; }

        public int? ProjectId { get; set; }

        public string? Notes { get; set; }
        public decimal? TaxPercent { get; set; }

        public string? CreatorDisplayName { get; set; }
        public bool ShowCreatorName { get; set; }
        public bool ShowSignature { get; set; }

        public List<CreateGeneratedInvoiceLineItemDTO> LineItems { get; set; } = new();
    }
}
