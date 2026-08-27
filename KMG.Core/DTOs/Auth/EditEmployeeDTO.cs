using KMG.Core.Enums;

namespace KMG.Core.DTOs.Auth
{
    public class EditEmployeeDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Password { get; set; }
        public string RoleId { get; set; } = string.Empty;
        public int? ManagerId { get; set; }
        public bool Suspended { get; set; }
        public EmployeeType EmployeeType { get; set; }
        public WageType WageType { get; set; }
        public decimal WageAmount { get; set; }
        public List<int>? AbilityIds { get; set; }
    }
}
