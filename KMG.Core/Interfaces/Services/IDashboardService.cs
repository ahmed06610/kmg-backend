using KMG.Core.DTOs.Dashboard;

namespace KMG.Core.Interfaces.Services
{
    public interface IDashboardService
    {
        Task<DashboardDTO> GetDashboardAsync(DateTime? from = null, DateTime? to = null);
    }
}
