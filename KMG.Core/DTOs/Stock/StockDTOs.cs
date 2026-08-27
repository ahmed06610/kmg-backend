namespace KMG.Core.DTOs.Stock
{
    public class MaterialDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal MinimumThreshold { get; set; }
        public bool IsLowStock { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class CreateMaterialDTO
    {
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public decimal MinimumThreshold { get; set; }
        public decimal InitialQuantity { get; set; }
    }

    public class UpdateMaterialDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public decimal MinimumThreshold { get; set; }
    }

    public class StockMovementDTO
    {
        public int Id { get; set; }
        public string MovementType { get; set; } = string.Empty;
        public int MaterialId { get; set; }
        public string MaterialName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPriceAtTime { get; set; }
        public int? ProjectId { get; set; }
        public string? ProjectCode { get; set; }
        public int? SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public DateTime MovementDate { get; set; }
        public string? Notes { get; set; }
        public string CreatedByEmployeeName { get; set; } = string.Empty;
    }

    public class CreatePurchaseDTO
    {
        public int MaterialId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public int SupplierId { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateIssueDTO
    {
        public int MaterialId { get; set; }
        public decimal Quantity { get; set; }
        public int ProjectId { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateReturnDTO
    {
        public int MaterialId { get; set; }
        public decimal Quantity { get; set; }
        public int ProjectId { get; set; }
        public string? Notes { get; set; }
    }
}
