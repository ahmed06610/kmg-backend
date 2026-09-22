using KMG.Core.DTOs.Supplier;

namespace KMG.Core.Interfaces.Services
{
    public interface ISupplierService
    {
        Task<List<SupplierListDTO>> GetAllAsync();
        Task<SupplierDetailsDTO?> GetByIdAsync(int id);
        Task<int> CreateAsync(CreateSupplierDTO model);
        Task<bool> UpdateAsync(UpdateSupplierDTO model);
        Task<bool> DeleteAsync(int id);
        Task<SupplierPaymentDTO> RecordPaymentAsync(CreateSupplierPaymentDTO model, int createdByEmployeeId);
        Task<SupplierPaymentDTO> UpdatePaymentAsync(UpdateSupplierPaymentDTO model, int employeeId);
        Task<bool> DeletePaymentAsync(int id);
        Task<List<SupplierPaymentDTO>> GetPendingChecksAsync();
        Task<SupplierPaymentDTO> ResolveCheckAsync(ResolveCheckDTO model, int employeeId);
    }
}
