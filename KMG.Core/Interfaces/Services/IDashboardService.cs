using KMG.Core.DTOs.Dashboard;

namespace KMG.Core.Interfaces.Services
{
    public interface IDashboardService
    {
        Task<DashboardDTO> GetDashboardAsync();
    }
}
