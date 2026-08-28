using KMG.Core.Enums;

namespace KMG.Core.DTOs.Employee
{
    public class EmployeeListDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public EmployeeType EmployeeType { get; set; }
        public WageType WageType { get; set; }
        public decimal WageAmount { get; set; }
        public string? ManagerName { get; set; }
        public bool Suspended { get; set; }
        public bool HasLoginAccount { get; set; }
        public decimal RemainingAdvances { get; set; }
    }

    public class CreateWorkerDTO
    {
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public EmployeeType EmployeeType { get; set; } = EmployeeType.Worker;
        public WageType WageType { get; set; } = WageType.Daily;
        public decimal WageAmount { get; set; }
        public int? ManagerId { get; set; }
    }
}
