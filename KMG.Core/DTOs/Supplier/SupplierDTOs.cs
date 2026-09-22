using KMG.Core.Enums;

namespace KMG.Core.DTOs.Supplier
{
    public class SupplierListDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public decimal TotalPurchases { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalRemaining { get; set; }
    }

    public class SupplierDetailsDTO : SupplierListDTO
    {
        public string? Address { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<SupplierPaymentDTO> Payments { get; set; } = new();
        public List<SupplierPurchaseDTO> Purchases { get; set; } = new();
    }

    public class SupplierPaymentDTO
    {
        public int Id { get; set; }
        public int SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public decimal Amount { get; set; }
        public decimal AmountCash { get; set; }
        public decimal AmountCredit { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? Notes { get; set; }
        public bool IsCheck { get; set; }
        public DateTime? CheckDueDate { get; set; }
        public string? CheckStatus { get; set; }
    }

    public class SupplierPurchaseDTO
    {
        public int Id { get; set; }
        public string MaterialName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPriceAtTime { get; set; }
        public DateTime MovementDate { get; set; }
    }

    public class CreateSupplierDTO
    {
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
    }

    public class UpdateSupplierDTO : CreateSupplierDTO
    {
        public int Id { get; set; }
    }

    public class CreateSupplierPaymentDTO
    {
        public int SupplierId { get; set; }
        public decimal AmountCash { get; set; }
        public decimal AmountCredit { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? Notes { get; set; }
        public bool IsCheck { get; set; }
        public DateTime? CheckDueDate { get; set; }
    }

    public class UpdateSupplierPaymentDTO
    {
        public int Id { get; set; }
        public decimal AmountCash { get; set; }
        public decimal AmountCredit { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? Notes { get; set; }
        public bool IsCheck { get; set; }
        public DateTime? CheckDueDate { get; set; }
    }

    public class ResolveCheckDTO
    {
        public int PaymentId { get; set; }
        public CheckResolutionAction Action { get; set; }
        public DateTime? NewDueDate { get; set; }
    }
}
