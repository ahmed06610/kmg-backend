using KMG.Core.DTOs.CashBox;

namespace KMG.Core.Interfaces.Services
{
    public interface ICustodyService
    {
        Task<List<CustodyDTO>> GetAllAsync();
        Task<CustodyDTO> CreateAsync(CreateCustodyDTO model, int createdByEmployeeId);
        Task<CustodyDTO> UpdateAsync(UpdateCustodyDTO model, int employeeId);
        Task<bool> DeleteAsync(int id, int employeeId);
        Task<CustodyDTO> SettleAsync(SettleCustodyDTO model, int settledByEmployeeId);
    }
}
