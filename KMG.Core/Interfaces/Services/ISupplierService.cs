using KMG.Core.DTOs.Supplier;

namespace KMG.Core.Interfaces.Services
{
    public interface ISupplierService
    {
        Task<List<SupplierListDTO>> GetAllAsync();
        Task<SupplierDetailsDTO?> GetByIdAsync(int id);
        Task<int> CreateAsync(CreateSupplierDTO model);
        Task<bool> UpdateAsync(UpdateSupplierDTO model);
        Task<SupplierPaymentDTO> RecordPaymentAsync(CreateSupplierPaymentDTO model, int createdByEmployeeId);
    }
}
