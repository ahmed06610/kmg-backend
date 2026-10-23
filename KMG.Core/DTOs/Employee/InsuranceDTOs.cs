namespace KMG.Core.DTOs.Employee
{
    public class EmployeeInsuranceDTO
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public decimal InsuranceAmount { get; set; }
    }

    public class InsurancePayoutDTO
    {
        public int Id { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime PaidDate { get; set; }
        public string? Notes { get; set; }
        public string CreatedByEmployeeName { get; set; } = string.Empty;
    }

    public class PayInsuranceDTO
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public string? Notes { get; set; }
    }
}
