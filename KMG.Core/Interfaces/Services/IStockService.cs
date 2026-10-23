using KMG.Core.DTOs.Stock;

namespace KMG.Core.Interfaces.Services
{
    public interface IStockService
    {
        Task<List<MaterialDTO>> GetAllMaterialsAsync();
        Task<MaterialDTO?> GetMaterialByIdAsync(int id);
        Task<int> CreateMaterialAsync(CreateMaterialDTO model, int createdByEmployeeId);
        Task<bool> UpdateMaterialAsync(UpdateMaterialDTO model);
        Task<bool> DeleteMaterialAsync(int id, bool force, int employeeId);
        Task<MaterialDeleteImpactDTO?> GetDeleteImpactAsync(int id);
        Task<MaterialDTO> MergeMaterialsAsync(MergeMaterialsDTO model, int employeeId);

        Task<List<MaterialCategoryDTO>> GetAllCategoriesAsync();
        Task<int> CreateCategoryAsync(CreateMaterialCategoryDTO model);
        Task<bool> UpdateCategoryAsync(UpdateMaterialCategoryDTO model);
        Task<bool> DeleteCategoryAsync(int id);

        Task<List<StockMovementDTO>> GetMovementsAsync(int? materialId = null, int? projectId = null);
        Task<List<StockPriceBatchDTO>> GetPriceBatchesAsync(int materialId);

        Task<StockMovementDTO> RecordPurchaseAsync(CreatePurchaseDTO model, int createdByEmployeeId);
        Task<StockMovementDTO> UpdatePurchaseAsync(UpdatePurchaseDTO model, int employeeId);
        Task<StockMovementDTO> IssueToProjectAsync(CreateIssueDTO model, int createdByEmployeeId);
        Task<StockMovementDTO> ReturnFromProjectAsync(CreateReturnDTO model, int createdByEmployeeId);
        Task<StockMovementDTO> CreateAdjustmentAsync(CreateStockAdjustmentDTO model, int employeeId);
        Task<StockMovementDTO> UpdateMovementAsync(UpdateStockMovementDTO model, int employeeId);
        Task<bool> DeleteMovementAsync(int id, int employeeId);
    }
}
