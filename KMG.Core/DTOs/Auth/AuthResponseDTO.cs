namespace KMG.Core.DTOs.Auth
{
    public class AuthResponseDTO
    {
        public bool IsAuthenticated { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresOn { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Name { get; set; }
        public int EmployeeId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public List<AbilityDTO> Abilities { get; set; } = new();
    }

    public class RoleDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
