using KMG.Core.DTOs.Employee;

namespace KMG.Core.Interfaces.Services
{
    public interface IMisrInsuranceService
    {
        Task<List<EmployeeMisrInsuranceDTO>> GetEmployeeMisrInsuranceSummaryAsync();
        Task<List<MisrInsurancePayoutDTO>> GetPayoutHistoryAsync();
        Task<MisrInsurancePayoutDTO> PayMisrInsuranceAsync(PayMisrInsuranceDTO model, int createdByEmployeeId);
    }
}
