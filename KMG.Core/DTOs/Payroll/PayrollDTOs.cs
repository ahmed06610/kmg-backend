using KMG.Core.Enums;

namespace KMG.Core.DTOs.Payroll
{
    public class AdvanceDTO
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal InstallmentAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public DateTime IssueDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class CreateAdvanceDTO
    {
        public int EmployeeId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal InstallmentAmount { get; set; }
        public string? Notes { get; set; }
    }

    public class PayrollAdjustmentDTO
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public bool Applied { get; set; }
    }

    public class CreateAdjustmentDTO
    {
        public int EmployeeId { get; set; }
        public AdjustmentType Type { get; set; }
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime Date { get; set; }
    }

    public class PayrollPreviewDTO
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public decimal BaseAmount { get; set; }
        public decimal MissionDoubleUpAmount { get; set; }
        public decimal DeductionsAmount { get; set; }
        public decimal BonusAmount { get; set; }
        public decimal AdvanceInstallmentAmount { get; set; }
        public decimal NetPaid { get; set; }
    }

    public class RunPayrollDTO
    {
        public int EmployeeId { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
    }

    public class PayrollPayoutDTO : PayrollPreviewDTO
    {
        public int Id { get; set; }
        public DateTime PaidDate { get; set; }
    }
}
