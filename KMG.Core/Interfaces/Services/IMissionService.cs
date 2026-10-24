using KMG.Core.DTOs.Mission;
using KMG.Core.Enums;

namespace KMG.Core.Interfaces.Services
{
    public interface IMissionService
    {
        Task<List<MissionDetailsDTO>> GetByProjectAsync(int projectId);
        Task<MissionDetailsDTO?> GetByIdAsync(int id);
        Task<List<MissionDetailsDTO>> GetRecentAsync(MissionStatus? status, int take);
        Task<int> CreateAsync(CreateMissionDTO model, int createdByEmployeeId);
        Task<MissionDetailsDTO> SettleAsync(SettleMissionDTO model, int settledByEmployeeId);
        Task<MissionDetailsDTO> UpdateAsync(UpdateMissionDTO model, int employeeId);
        Task<bool> DeleteAsync(int id, int employeeId);
    }
}
