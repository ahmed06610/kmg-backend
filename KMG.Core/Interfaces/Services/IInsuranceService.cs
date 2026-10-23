using KMG.Core.DTOs.Employee;

namespace KMG.Core.Interfaces.Services
{
    public interface IInsuranceService
    {
        Task<List<EmployeeInsuranceDTO>> GetEmployeeInsuranceSummaryAsync();
        Task<List<InsurancePayoutDTO>> GetPayoutHistoryAsync();
        Task<InsurancePayoutDTO> PayInsuranceAsync(PayInsuranceDTO model, int createdByEmployeeId);
    }
}
