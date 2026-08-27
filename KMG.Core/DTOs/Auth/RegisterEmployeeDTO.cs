using KMG.Core.Enums;

namespace KMG.Core.DTOs.Auth
{
    public class RegisterEmployeeDTO
    {
        public string UserName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string Password { get; set; } = string.Empty;
        public string RoleId { get; set; } = string.Empty;
        public EmployeeType EmployeeType { get; set; }
        public WageType WageType { get; set; }
        public decimal WageAmount { get; set; }
        public int? ManagerId { get; set; }
        public List<int>? AbilityIds { get; set; }
    }
}
