using KMG.Core.DTOs.Stock;

namespace KMG.Core.Interfaces.Services
{
    public interface IStockService
    {
        Task<List<MaterialDTO>> GetAllMaterialsAsync();
        Task<MaterialDTO?> GetMaterialByIdAsync(int id);
        Task<int> CreateMaterialAsync(CreateMaterialDTO model, int createdByEmployeeId);
        Task<bool> UpdateMaterialAsync(UpdateMaterialDTO model);
        Task<bool> DeleteMaterialAsync(int id);

        Task<List<MaterialCategoryDTO>> GetAllCategoriesAsync();
        Task<int> CreateCategoryAsync(CreateMaterialCategoryDTO model);
        Task<bool> UpdateCategoryAsync(UpdateMaterialCategoryDTO model);
        Task<bool> DeleteCategoryAsync(int id);

        Task<List<StockMovementDTO>> GetMovementsAsync(int? materialId = null, int? projectId = null);

        Task<StockMovementDTO> RecordPurchaseAsync(CreatePurchaseDTO model, int createdByEmployeeId);
        Task<StockMovementDTO> IssueToProjectAsync(CreateIssueDTO model, int createdByEmployeeId);
        Task<StockMovementDTO> ReturnFromProjectAsync(CreateReturnDTO model, int createdByEmployeeId);
    }
}
