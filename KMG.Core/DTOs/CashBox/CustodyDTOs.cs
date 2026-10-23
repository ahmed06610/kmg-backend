namespace KMG.Core.DTOs.CashBox
{
    public class CustodyDTO
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public decimal AmountCash { get; set; }
        public decimal AmountCredit { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime IssueDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal? SettledAmount { get; set; }
        public DateTime? SettledDate { get; set; }
        public string? Notes { get; set; }

        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;

        public int? ProjectId { get; set; }
        public string? ProjectName { get; set; }
    }

    public class CreateCustodyDTO
    {
        public int EmployeeId { get; set; }
        public decimal AmountCash { get; set; }
        public decimal AmountCredit { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime IssueDate { get; set; }
        public int? ProjectId { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateCustodyDTO
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public decimal AmountCash { get; set; }
        public decimal AmountCredit { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime IssueDate { get; set; }
        public int? ProjectId { get; set; }
        public string? Notes { get; set; }
    }

    public class SettleCustodyDTO
    {
        public int CustodyId { get; set; }
        public decimal SettledAmount { get; set; }
        public DateTime SettledDate { get; set; }
        public string? Notes { get; set; }
    }
}
