using KMG.Core.DTOs.Invoice;

namespace KMG.Core.Interfaces.Services
{
    public interface IInvoiceService
    {
        Task<PagedResultDTO<InvoiceDTO>> GetInvoicesAsync(InvoiceFilterDTO filter);

        Task<List<GeneratedInvoiceListDTO>> GetGeneratedInvoicesAsync();
        Task<GeneratedInvoiceDTO?> GetGeneratedInvoiceByIdAsync(int id);
        Task<GeneratedInvoiceDTO> CreateGeneratedInvoiceAsync(CreateGeneratedInvoiceDTO model, int createdByEmployeeId);
        Task<bool> DeleteGeneratedInvoiceAsync(int id);
    }
}
