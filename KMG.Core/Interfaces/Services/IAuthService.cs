using KMG.Core.DTOs.Auth;

namespace KMG.Core.Interfaces.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDTO> LoginAsync(LoginDTO model);
        Task<AuthResponseDTO> RegisterEmployeeAsync(RegisterEmployeeDTO model);
        Task<AuthResponseDTO> EditEmployeeAsync(EditEmployeeDTO model);
        Task<List<RoleDto>> GetAllRolesAsync();
        Task<List<AbilitySelectionDTO>> GetAbilitiesForRoleAsync(string roleId);
    }
}
