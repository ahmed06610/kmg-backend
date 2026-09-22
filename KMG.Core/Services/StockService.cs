using KMG.Core.DTOs.Stock;
using KMG.Core.Enums;
using KMG.Core.Helper;
using KMG.Core.Interfaces;
using KMG.Core.Interfaces.Services;
using KMG.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace KMG.Core.Services
{
    public class StockService : IStockService
    {
        private readonly IUnitOfWork _unitOfWork;

        public StockService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<MaterialDTO>> GetAllMaterialsAsync()
        {
            var materials = await _unitOfWork.Material.GetQueryable(null).Include(m => m.Category).ToListAsync();
            return materials.Select(MapMaterial).OrderBy(m => m.Name).ToList();
        }

        public async Task<MaterialDTO?> GetMaterialByIdAsync(int id)
        {
            var material = await _unitOfWork.Material.GetQueryable(m => m.Id == id).Include(m => m.Category).FirstOrDefaultAsync();
            return material == null ? null : MapMaterial(material);
        }

        public async Task<int> CreateMaterialAsync(CreateMaterialDTO model, int createdByEmployeeId)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var extraFieldValues = await ValidateExtraFieldValuesAsync(model.CategoryId, model.ExtraFieldValues);

                var material = new Material
                {
                    Name = model.Name,
                    Unit = model.Unit,
                    UnitPrice = model.UnitPrice,
                    MinimumThreshold = model.MinimumThreshold,
                    Quantity = model.InitialQuantity,
                    CategoryId = model.CategoryId,
                    ExtraFieldValues = extraFieldValues,
                    LastUpdated = TimeHelper.NowInEgypt
                };

                await _unitOfWork.Material.AddAsync(material);
                await _unitOfWork.CompleteAsync(); // نحتاج material.Id عشان نربط بيه حركة الرصيد الافتتاحي

                if (model.InitialQuantity > 0)
                {
                    await _unitOfWork.StockMovement.AddAsync(new StockMovement
                    {
                        MaterialId = material.Id,
                        MovementType = MovementType.OpeningBalance,
                        Quantity = model.InitialQuantity,
                        UnitPriceAtTime = model.UnitPrice,
                        MovementDate = TimeHelper.NowInEgypt,
                        Notes = "رصيد افتتاحي عند إنشاء الخامة",
                        CreatedByEmployeeId = createdByEmployeeId
                    });
                    await _unitOfWork.CompleteAsync();
                }

                await transaction.CommitAsync();
                return material.Id;
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> UpdateMaterialAsync(UpdateMaterialDTO model)
        {
            var material = await _unitOfWork.Material.GetByIdAsync(model.Id);
            if (material == null) return false;

            var extraFieldValues = await ValidateExtraFieldValuesAsync(model.CategoryId, model.ExtraFieldValues);

            material.Name = model.Name;
            material.Unit = model.Unit;
            material.UnitPrice = model.UnitPrice;
            material.MinimumThreshold = model.MinimumThreshold;
            material.CategoryId = model.CategoryId;
            material.ExtraFieldValues = extraFieldValues;
            material.LastUpdated = TimeHelper.NowInEgypt;

            _unitOfWork.Material.Update(material);
            await _unitOfWork.CompleteAsync();
            return true;
        }

        private async Task<Dictionary<string, string>> ValidateExtraFieldValuesAsync(int? categoryId, Dictionary<string, string>? values)
        {
            if (categoryId == null) return new Dictionary<string, string>();

            var category = await _unitOfWork.MaterialCategory.GetByIdAsync(categoryId.Value)
                ?? throw new Exception("الفئة غير موجودة");

            var allowedKeys = category.ExtraFieldDefinitions.Select(f => f.Key).ToHashSet();
            var result = new Dictionary<string, string>();
            foreach (var (key, value) in values ?? new Dictionary<string, string>())
            {
                if (!allowedKeys.Contains(key))
                    throw new Exception($"الحقل \"{key}\" مش معرّف في فئة \"{category.Name}\"");
                result[key] = value;
            }
            return result;
        }

        public async Task<bool> DeleteMaterialAsync(int id)
        {
            var material = await _unitOfWork.Material.GetByIdAsync(id);
            if (material == null) return false;

            try
            {
                _unitOfWork.Material.Delete(material);
                await _unitOfWork.CompleteAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                throw new Exception("لا يمكن حذف خامة لها حركات مخزون مسجلة (شراء/صرف/مرتجع) في النظام");
            }
        }

        public async Task<List<MaterialCategoryDTO>> GetAllCategoriesAsync()
        {
            var categories = await _unitOfWork.MaterialCategory.GetQueryable(null).Include(c => c.Materials).ToListAsync();
            return categories.Select(MapCategory).OrderBy(c => c.Name).ToList();
        }

        public async Task<int> CreateCategoryAsync(CreateMaterialCategoryDTO model)
        {
            var category = new MaterialCategory
            {
                Name = model.Name,
                ExtraFieldDefinitions = model.ExtraFieldDefinitions
                    .Select(f => new CategoryFieldDefinition { Key = f.Key, Label = f.Label, FieldType = f.FieldType })
                    .ToList()
            };

            await _unitOfWork.MaterialCategory.AddAsync(category);
            await _unitOfWork.CompleteAsync();
            return category.Id;
        }

        public async Task<bool> UpdateCategoryAsync(UpdateMaterialCategoryDTO model)
        {
            var category = await _unitOfWork.MaterialCategory.GetByIdAsync(model.Id);
            if (category == null) return false;

            var existingKeys = category.ExtraFieldDefinitions.Select(f => f.Key).ToHashSet();
            var newKeys = model.ExtraFieldDefinitions.Select(f => f.Key).ToHashSet();
            if (!existingKeys.IsSubsetOf(newKeys))
                throw new Exception("لا يمكن حذف أو تغيير مفتاح حقل موجود بالفعل في الفئة - يمكن فقط إضافة حقول جديدة");

            category.Name = model.Name;
            category.ExtraFieldDefinitions = model.ExtraFieldDefinitions
                .Select(f => new CategoryFieldDefinition { Key = f.Key, Label = f.Label, FieldType = f.FieldType })
                .ToList();

            _unitOfWork.MaterialCategory.Update(category);
            await _unitOfWork.CompleteAsync();
            return true;
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            var category = await _unitOfWork.MaterialCategory.GetByIdAsync(id);
            if (category == null) return false;

            try
            {
                _unitOfWork.MaterialCategory.Delete(category);
                await _unitOfWork.CompleteAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                throw new Exception("لا يمكن حذف فئة لها أصناف مسجلة عليها");
            }
        }

        private static MaterialCategoryDTO MapCategory(MaterialCategory c) => new()
        {
            Id = c.Id,
            Name = c.Name,
            ExtraFieldDefinitions = c.ExtraFieldDefinitions
                .Select(f => new CategoryFieldDefinitionDTO { Key = f.Key, Label = f.Label, FieldType = f.FieldType })
                .ToList(),
            MaterialsCount = c.Materials.Count
        };

        public async Task<List<StockMovementDTO>> GetMovementsAsync(int? materialId = null, int? projectId = null)
        {
            var query = _unitOfWork.StockMovement.GetQueryable(m =>
                (materialId == null || m.MaterialId == materialId) &&
                (projectId == null || m.ProjectId == projectId));

            var movements = await query
                .Include(m => m.Material)
                .Include(m => m.Project)
                .Include(m => m.Supplier)
                .Include(m => m.CreatedByEmployee)
                .OrderByDescending(m => m.MovementDate)
                .ToListAsync();

            return movements.Select(MapMovement).ToList();
        }

        public async Task<StockMovementDTO> RecordPurchaseAsync(CreatePurchaseDTO model, int createdByEmployeeId)
        {
            var material = await _unitOfWork.Material.GetByIdAsync(model.MaterialId)
                ?? throw new Exception("الخامة غير موجودة");

            material.Quantity += model.Quantity;
            material.UnitPrice = model.UnitPrice; // آخر سعر شراء يبقى السعر الحالي للخامة
            material.LastUpdated = TimeHelper.NowInEgypt;
            _unitOfWork.Material.Update(material);

            var movement = new StockMovement
            {
                MaterialId = model.MaterialId,
                MovementType = MovementType.Purchase,
                Quantity = model.Quantity,
                UnitPriceAtTime = model.UnitPrice,
                SupplierId = model.SupplierId,
                MovementDate = TimeHelper.NowInEgypt,
                Notes = model.Notes,
                CreatedByEmployeeId = createdByEmployeeId
            };

            await _unitOfWork.StockMovement.AddAsync(movement);
            await _unitOfWork.CompleteAsync();

            return await LoadMovementDTO(movement.Id);
        }

        public async Task<StockMovementDTO> IssueToProjectAsync(CreateIssueDTO model, int createdByEmployeeId)
        {
            var material = await _unitOfWork.Material.GetByIdAsync(model.MaterialId)
                ?? throw new Exception("الخامة غير موجودة");

            if (material.Quantity < model.Quantity)
                throw new Exception($"لا يوجد ما يكفي من الخامة \"{material.Name}\" في المخزن (المتاح: {material.Quantity} {material.Unit})");

            material.Quantity -= model.Quantity;
            material.LastUpdated = TimeHelper.NowInEgypt;
            _unitOfWork.Material.Update(material);

            var movement = new StockMovement
            {
                MaterialId = model.MaterialId,
                MovementType = MovementType.IssueToProject,
                Quantity = model.Quantity,
                UnitPriceAtTime = material.UnitPrice,
                ProjectId = model.ProjectId,
                MovementDate = TimeHelper.NowInEgypt,
                Notes = model.Notes,
                CreatedByEmployeeId = createdByEmployeeId
            };

            await _unitOfWork.StockMovement.AddAsync(movement);
            await _unitOfWork.CompleteAsync();

            return await LoadMovementDTO(movement.Id);
        }

        public async Task<StockMovementDTO> ReturnFromProjectAsync(CreateReturnDTO model, int createdByEmployeeId)
        {
            var material = await _unitOfWork.Material.GetByIdAsync(model.MaterialId)
                ?? throw new Exception("الخامة غير موجودة");

            var projectMovements = await _unitOfWork.StockMovement
                .FindAllAsync(m => m.MaterialId == model.MaterialId && m.ProjectId == model.ProjectId
                    && (m.MovementType == MovementType.IssueToProject || m.MovementType == MovementType.ReturnFromProject));

            var netIssued = projectMovements.Where(m => m.MovementType == MovementType.IssueToProject).Sum(m => m.Quantity)
                - projectMovements.Where(m => m.MovementType == MovementType.ReturnFromProject).Sum(m => m.Quantity);

            if (model.Quantity > netIssued)
                throw new Exception($"الكمية المرتجعة ({model.Quantity}) أكبر من صافي المصروف الفعلي لهذا المشروع من الخامة ({netIssued})");

            material.Quantity += model.Quantity;
            material.LastUpdated = TimeHelper.NowInEgypt;
            _unitOfWork.Material.Update(material);

            var movement = new StockMovement
            {
                MaterialId = model.MaterialId,
                MovementType = MovementType.ReturnFromProject,
                Quantity = model.Quantity,
                UnitPriceAtTime = material.UnitPrice,
                ProjectId = model.ProjectId,
                MovementDate = TimeHelper.NowInEgypt,
                Notes = model.Notes,
                CreatedByEmployeeId = createdByEmployeeId
            };

            await _unitOfWork.StockMovement.AddAsync(movement);
            await _unitOfWork.CompleteAsync();

            return await LoadMovementDTO(movement.Id);
        }

        private async Task<StockMovementDTO> LoadMovementDTO(int id)
        {
            var movement = await _unitOfWork.StockMovement.GetQueryable(m => m.Id == id)
                .Include(m => m.Material)
                .Include(m => m.Project)
                .Include(m => m.Supplier)
                .Include(m => m.CreatedByEmployee)
                .FirstAsync();

            return MapMovement(movement);
        }

        private static MaterialDTO MapMaterial(Material m) => new()
        {
            Id = m.Id,
            Name = m.Name,
            Unit = m.Unit,
            Quantity = m.Quantity,
            UnitPrice = m.UnitPrice,
            MinimumThreshold = m.MinimumThreshold,
            IsLowStock = m.IsLowStock,
            LastUpdated = m.LastUpdated,
            TotalPrice = m.TotalPrice,
            CategoryId = m.CategoryId,
            CategoryName = m.Category?.Name,
            ExtraFieldValues = m.ExtraFieldValues
        };

        private static StockMovementDTO MapMovement(StockMovement m) => new()
        {
            Id = m.Id,
            MovementType = m.MovementType.ToString(),
            MaterialId = m.MaterialId,
            MaterialName = m.Material.Name,
            Quantity = m.Quantity,
            UnitPriceAtTime = m.UnitPriceAtTime,
            ProjectId = m.ProjectId,
            ProjectCode = m.Project?.ProjectCode,
            ProjectName = m.Project?.Name,
            SupplierId = m.SupplierId,
            SupplierName = m.Supplier?.Name,
            MovementDate = m.MovementDate,
            Notes = m.Notes,
            CreatedByEmployeeName = m.CreatedByEmployee.Name
        };
    }
}
