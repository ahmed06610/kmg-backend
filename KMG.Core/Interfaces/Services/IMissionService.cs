using KMG.Core.DTOs.Mission;

namespace KMG.Core.Interfaces.Services
{
    public interface IMissionService
    {
        Task<List<MissionListDTO>> GetByProjectAsync(int projectId);
        Task<MissionDetailsDTO?> GetByIdAsync(int id);
        Task<int> CreateAsync(CreateMissionDTO model, int createdByEmployeeId);
        Task<MissionDetailsDTO> SettleAsync(SettleMissionDTO model, int settledByEmployeeId);
        Task<MissionDetailsDTO> UpdateAsync(UpdateMissionDTO model, int employeeId);
        Task<bool> DeleteAsync(int id, int employeeId);
    }
}
