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
        public decimal TotalPrice { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public Dictionary<string, string> ExtraFieldValues { get; set; } = new();
    }

    public class CreateMaterialDTO
    {
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public decimal MinimumThreshold { get; set; }
        public decimal InitialQuantity { get; set; }
        public int? CategoryId { get; set; }
        public Dictionary<string, string>? ExtraFieldValues { get; set; }
    }

    public class UpdateMaterialDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public decimal MinimumThreshold { get; set; }
        public int? CategoryId { get; set; }
        public Dictionary<string, string>? ExtraFieldValues { get; set; }
    }

    public class CategoryFieldDefinitionDTO
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string FieldType { get; set; } = "text";
    }

    public class MaterialCategoryDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<CategoryFieldDefinitionDTO> ExtraFieldDefinitions { get; set; } = new();
        public int MaterialsCount { get; set; }
    }

    public class CreateMaterialCategoryDTO
    {
        public string Name { get; set; } = string.Empty;
        public List<CategoryFieldDefinitionDTO> ExtraFieldDefinitions { get; set; } = new();
    }

    public class UpdateMaterialCategoryDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<CategoryFieldDefinitionDTO> ExtraFieldDefinitions { get; set; } = new();
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
        public string? ProjectName { get; set; }
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
