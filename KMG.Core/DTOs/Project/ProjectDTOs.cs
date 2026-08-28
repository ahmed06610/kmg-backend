using KMG.Core.Enums;

namespace KMG.Core.DTOs.Project
{
    public class ProjectListDTO
    {
        public int Id { get; set; }
        public string ProjectCode { get; set; } = string.Empty;
        public string ProjectType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public decimal ContractValue { get; set; }
        public decimal TotalCollected { get; set; }
        public decimal RemainingBalance { get; set; }
        public decimal NetProfit { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ProjectDetailsDTO : ProjectListDTO
    {
        public int ClientId { get; set; }
        public string? ClientPhone { get; set; }
        public string? ClientEmail { get; set; }
        public string? ClientAddress { get; set; }
        public string? Description { get; set; }
        public decimal? TenderInsuranceAmount { get; set; }
        public decimal? TenderTaxAmount { get; set; }
        public decimal? SupplyProfitMargin { get; set; }
        public decimal TotalMaterialsCost { get; set; }
        public decimal TotalPettyExpenses { get; set; }
        public decimal TotalLaborCost { get; set; }

        public List<ProjectPaymentDTO> Payments { get; set; } = new();
        public List<ProjectExpenseDTO> Expenses { get; set; } = new();
        public List<Stock.StockMovementDTO> StockMovements { get; set; } = new();
        public List<ProjectAttachmentDTO> Attachments { get; set; } = new();
        public List<ProjectAuditDTO> AuditLogs { get; set; } = new();
    }

    public class ProjectPaymentDTO
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public decimal AmountCash { get; set; }
        public decimal AmountCredit { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? Notes { get; set; }
    }

    public class ProjectExpenseDTO
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string Category { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime ExpenseDate { get; set; }
        public string? AttachmentUrl { get; set; }
    }

    public class ProjectAttachmentDTO
    {
        public int Id { get; set; }
        public string FileUrl { get; set; } = string.Empty;
        public string? FileName { get; set; }
        public string? Description { get; set; }
        public DateTime UploadedAt { get; set; }
        public string UploadedByEmployeeName { get; set; } = string.Empty;
    }

    public class ProjectAuditDTO
    {
        public int Id { get; set; }
        public string ActionDescription { get; set; } = string.Empty;
        public DateTime ActionDate { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
    }

    public class CreateProjectDTO
    {
        public ProjectType ProjectType { get; set; }
        public int ClientId { get; set; }
        public decimal ContractValue { get; set; }
        public string? Description { get; set; }
        public decimal? TenderInsuranceAmount { get; set; }
        public decimal? TenderTaxAmount { get; set; }
        public decimal? SupplyProfitMargin { get; set; }
    }

    public class UpdateProjectStatusDTO
    {
        public int ProjectId { get; set; }
        public ProjectStatus Status { get; set; }
    }

    public class CreateProjectPaymentDTO
    {
        public int ProjectId { get; set; }
        public decimal AmountCash { get; set; }
        public decimal AmountCredit { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateProjectExpenseDTO
    {
        public int ProjectId { get; set; }
        public decimal Amount { get; set; }
        public ExpenseCategory Category { get; set; }
        public string? Description { get; set; }
        public DateTime ExpenseDate { get; set; }
        public string? AttachmentUrl { get; set; }
        public bool PaidFromCashBox { get; set; } = true;
    }

    public class CreateProjectAttachmentDTO
    {
        public int ProjectId { get; set; }
        public string FileUrl { get; set; } = string.Empty;
        public string? FileName { get; set; }
        public string? Description { get; set; }
    }
}
