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
        private readonly ICashBoxService _cashBoxService;

        public StockService(IUnitOfWork unitOfWork, ICashBoxService cashBoxService)
        {
            _unitOfWork = unitOfWork;
            _cashBoxService = cashBoxService;
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

        public async Task<MaterialDeleteImpactDTO?> GetDeleteImpactAsync(int id)
        {
            if (await _unitOfWork.Material.GetByIdAsync(id) == null) return null;

            var movements = await _unitOfWork.StockMovement.GetQueryable(m => m.MaterialId == id)
                .Include(m => m.Project)
                .Include(m => m.Supplier)
                .ToListAsync();

            return new MaterialDeleteImpactDTO
            {
                MovementsCount = movements.Count,
                PurchasesCount = movements.Count(m => m.MovementType == MovementType.Purchase),
                IssuesCount = movements.Count(m => m.MovementType == MovementType.IssueToProject),
                ReturnsCount = movements.Count(m => m.MovementType == MovementType.ReturnFromProject),
                AffectedProjects = movements.Where(m => m.Project != null).Select(m => $"{m.Project!.Name} ({m.Project.ProjectCode})").Distinct().ToList(),
                AffectedSuppliers = movements.Where(m => m.Supplier != null).Select(m => m.Supplier!.Name).Distinct().ToList(),
                TransportCostToReverse = movements.Sum(m => m.TransportCost)
            };
        }

        /// <param name="force">
        /// لو الخامة عليها حركات: من غير force الحذف بيترفض برسالة واضحة، ومع force بيتمسح كل حركاتها
        /// (ومعاها أثرها في الخزنة زي مصروف النقل) - تكلفة المشاريع ومستحقات الموردين بيتعاد حسابها تلقائيًا
        /// لأنها محسوبة لحظيًا من الحركات نفسها
        /// </param>
        public async Task<bool> DeleteMaterialAsync(int id, bool force, int employeeId)
        {
            var material = await _unitOfWork.Material.GetByIdAsync(id);
            if (material == null) return false;

            var movements = await _unitOfWork.StockMovement.GetQueryable(m => m.MaterialId == id).ToListAsync();
            if (movements.Count > 0 && !force)
                throw new Exception($"الخامة \"{material.Name}\" عليها {movements.Count} حركة مخزون مسجلة - أكّد الحذف الكامل عشان تتمسح الحركات دي كمان، أو استخدم \"دمج\" لو الخامة مكررة");

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                if (movements.Count > 0)
                {
                    var movementIds = movements.Select(m => m.Id).ToList();
                    await _cashBoxService.ReverseAsync(t => t.StockMovementId != null && movementIds.Contains(t.StockMovementId.Value));

                    foreach (var projectId in movements.Where(m => m.ProjectId != null).Select(m => m.ProjectId!.Value).Distinct())
                        await AddProjectAuditAsync(projectId, employeeId, $"حذف خامة \"{material.Name}\" من المخزن بالكامل (ومعاها حركات الصرف/المرتجع على المشروع)");

                    _unitOfWork.StockMovement.DeleteRange(movements);
                }

                _unitOfWork.Material.Delete(material);
                await _unitOfWork.CompleteAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<MaterialDTO> MergeMaterialsAsync(MergeMaterialsDTO model, int employeeId)
        {
            if (model.SourceMaterialId == model.TargetMaterialId)
                throw new Exception("لازم تختار خامة مختلفة تدمج فيها");

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var source = await _unitOfWork.Material.GetByIdAsync(model.SourceMaterialId) ?? throw new Exception("الخامة المراد دمجها غير موجودة");
                var target = await _unitOfWork.Material.GetByIdAsync(model.TargetMaterialId) ?? throw new Exception("الخامة الهدف غير موجودة");

                var movements = await _unitOfWork.StockMovement.GetQueryable(m => m.MaterialId == source.Id).ToListAsync();
                foreach (var movement in movements)
                {
                    movement.MaterialId = target.Id;
                    _unitOfWork.StockMovement.Update(movement);
                }

                foreach (var projectId in movements.Where(m => m.ProjectId != null).Select(m => m.ProjectId!.Value).Distinct())
                    await AddProjectAuditAsync(projectId, employeeId, $"دمج خامة \"{source.Name}\" في خامة \"{target.Name}\" (نقل حركاتها على المشروع)");

                _unitOfWork.Material.Delete(source);
                await _unitOfWork.CompleteAsync();

                await RecalculateMaterialAsync(target.Id, recalculatePrice: true);
                await _unitOfWork.CompleteAsync();
                await transaction.CommitAsync();

                return (await GetMaterialByIdAsync(target.Id))!;
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<StockMovementDTO> CreateAdjustmentAsync(CreateStockAdjustmentDTO model, int employeeId)
        {
            if (model.ActualQuantity < 0)
                throw new Exception("الكمية الفعلية لا يمكن أن تكون سالبة");

            var material = await _unitOfWork.Material.GetByIdAsync(model.MaterialId) ?? throw new Exception("الخامة غير موجودة");
            var difference = model.ActualQuantity - material.Quantity;
            if (difference == 0)
                throw new Exception("الكمية الفعلية مطابقة للرصيد المسجل - مفيش فرق يتسوى");

            var movement = new StockMovement
            {
                MaterialId = material.Id,
                MovementType = MovementType.Adjustment,
                Quantity = difference,
                UnitPriceAtTime = material.UnitPrice,
                MovementDate = TimeHelper.NowInEgypt,
                Notes = string.IsNullOrWhiteSpace(model.Notes)
                    ? $"تسوية جرد: الرصيد كان {material.Quantity} وأصبح {model.ActualQuantity}"
                    : model.Notes,
                CreatedByEmployeeId = employeeId
            };

            material.Quantity = model.ActualQuantity;
            material.LastUpdated = TimeHelper.NowInEgypt;
            _unitOfWork.Material.Update(material);

            await _unitOfWork.StockMovement.AddAsync(movement);
            await _unitOfWork.CompleteAsync();

            return await LoadMovementDTO(movement.Id);
        }

        public async Task<StockMovementDTO> UpdateMovementAsync(UpdateStockMovementDTO model, int employeeId)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var movement = await _unitOfWork.StockMovement.GetQueryable(m => m.Id == model.Id)
                    .Include(m => m.Material)
                    .FirstOrDefaultAsync() ?? throw new Exception("الحركة غير موجودة");

                if (movement.MovementType == MovementType.Purchase)
                    throw new Exception("حركة الشراء بتتعدل من \"تعديل خطأ في عملية شراء\" عشان مربوطة بالمورد ومصروف النقل");

                if (movement.MovementType == MovementType.Adjustment ? model.Quantity == 0 : model.Quantity <= 0)
                    throw new Exception("الكمية يجب أن تكون أكبر من صفر");

                var oldProjectId = movement.ProjectId;
                var oldPrice = movement.UnitPriceAtTime;
                var oldQuantity = movement.Quantity;

                switch (movement.MovementType)
                {
                    case MovementType.IssueToProject:
                    case MovementType.ReturnFromProject:
                        if (model.ProjectId == null || await _unitOfWork.Project.GetByIdAsync(model.ProjectId.Value) == null)
                            throw new Exception("لازم تختار مشروع صحيح");
                        movement.ProjectId = model.ProjectId;
                        if (movement.MovementType == MovementType.IssueToProject)
                        {
                            if (model.UnitPrice < 0) throw new Exception("السعر لا يمكن أن يكون سالبًا");
                            movement.UnitPriceAtTime = model.UnitPrice;
                        }
                        break;

                    case MovementType.OpeningBalance:
                        if (model.UnitPrice < 0) throw new Exception("السعر لا يمكن أن يكون سالبًا");
                        movement.UnitPriceAtTime = model.UnitPrice;
                        break;
                }

                movement.Quantity = model.Quantity;
                movement.Notes = model.Notes;
                if (model.MovementDate.HasValue) movement.MovementDate = model.MovementDate.Value;
                _unitOfWork.StockMovement.Update(movement);
                await _unitOfWork.CompleteAsync();

                await RecalculateMaterialAsync(movement.MaterialId, recalculatePrice: movement.MovementType == MovementType.OpeningBalance);

                // بعد ما الحركة اتحفظت، نتأكد إن التعديل مخلاش أي رصيد بالسالب (المخزن/دفعة السعر/صافي صرف المشروع)
                if (movement.MovementType == MovementType.IssueToProject)
                    await EnsurePriceBatchNotOverdrawnAsync(movement.MaterialId, movement.UnitPriceAtTime);
                if (movement.MovementType == MovementType.OpeningBalance)
                    await EnsurePriceBatchNotOverdrawnAsync(movement.MaterialId, oldPrice);
                if (movement.MovementType is MovementType.IssueToProject or MovementType.ReturnFromProject)
                {
                    await EnsureProjectNetIssuedNotNegativeAsync(movement.MaterialId, oldProjectId);
                    await EnsureProjectNetIssuedNotNegativeAsync(movement.MaterialId, movement.ProjectId);
                }

                var description = $"تعديل حركة {MovementLabel(movement.MovementType)} لخامة \"{movement.Material.Name}\": الكمية من {oldQuantity} إلى {movement.Quantity}";
                if (oldProjectId != null) await AddProjectAuditAsync(oldProjectId.Value, employeeId, description);
                if (movement.ProjectId != null && movement.ProjectId != oldProjectId) await AddProjectAuditAsync(movement.ProjectId.Value, employeeId, description);

                await _unitOfWork.CompleteAsync();
                await transaction.CommitAsync();

                return await LoadMovementDTO(movement.Id);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> DeleteMovementAsync(int id, int employeeId)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var movement = await _unitOfWork.StockMovement.GetQueryable(m => m.Id == id)
                    .Include(m => m.Material)
                    .FirstOrDefaultAsync();
                if (movement == null) return false;

                // أي أثر في الخزنة مربوط بالحركة دي (مصروف النقل في حالة الشراء) بيتلغي معاها
                await _cashBoxService.ReverseAsync(t => t.StockMovementId == id);
                _unitOfWork.StockMovement.Delete(movement);
                await _unitOfWork.CompleteAsync();

                var isPriced = movement.MovementType is MovementType.Purchase or MovementType.OpeningBalance;
                await RecalculateMaterialAsync(movement.MaterialId, recalculatePrice: isPriced);

                if (isPriced)
                    await EnsurePriceBatchNotOverdrawnAsync(movement.MaterialId, movement.UnitPriceAtTime);
                if (movement.MovementType is MovementType.IssueToProject or MovementType.ReturnFromProject)
                    await EnsureProjectNetIssuedNotNegativeAsync(movement.MaterialId, movement.ProjectId);

                if (movement.ProjectId != null)
                    await AddProjectAuditAsync(movement.ProjectId.Value, employeeId,
                        $"حذف حركة {MovementLabel(movement.MovementType)} لخامة \"{movement.Material.Name}\" بكمية {movement.Quantity}");

                await _unitOfWork.CompleteAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        // صرف لمشروع بالسالب، وأي حركة تانية بالموجب (تسوية الجرد كميتها أصلًا بالإشارة)
        private static decimal SignedQuantity(StockMovement m) =>
            m.MovementType == MovementType.IssueToProject ? -m.Quantity : m.Quantity;

        /// <summary>
        /// بيعيد حساب رصيد الخامة (وسعرها لو recalculatePrice) من واقع الحركات المحفوظة فعليًا بدل الجمع/الطرح
        /// التراكمي - بينادى بعد أي تعديل أو حذف لحركة قديمة عشان الرصيد يفضل مطابق للسجل مهما اتعدل.
        /// </summary>
        private async Task RecalculateMaterialAsync(int materialId, bool recalculatePrice)
        {
            var material = await _unitOfWork.Material.GetByIdAsync(materialId) ?? throw new Exception("الخامة غير موجودة");
            var movements = await _unitOfWork.StockMovement.GetQueryable(m => m.MaterialId == materialId).ToListAsync();

            var quantity = movements.Sum(SignedQuantity);
            if (quantity < 0)
                throw new Exception($"العملية دي هتخلي رصيد \"{material.Name}\" في المخزن بالسالب ({quantity}) - الكمية دي اتصرف منها بالفعل، عدّل أو احذف حركات الصرف الأول");

            material.Quantity = quantity;
            if (recalculatePrice)
            {
                var latestPriced = movements
                    .Where(m => m.MovementType is MovementType.Purchase or MovementType.OpeningBalance)
                    .OrderByDescending(m => m.MovementDate).ThenByDescending(m => m.Id)
                    .FirstOrDefault();
                if (latestPriced != null) material.UnitPrice = latestPriced.UnitPriceAtTime;
            }
            material.LastUpdated = TimeHelper.NowInEgypt;
            _unitOfWork.Material.Update(material);
        }

        private async Task EnsurePriceBatchNotOverdrawnAsync(int materialId, decimal unitPrice)
        {
            var movements = await _unitOfWork.StockMovement
                .GetQueryable(m => m.MaterialId == materialId && m.UnitPriceAtTime == unitPrice)
                .ToListAsync();

            var available = movements.Sum(SignedQuantity);
            if (available < 0)
                throw new Exception($"الكمية المتاحة بسعر {unitPrice:N2} مش كفاية - اتصرف منها على مشاريع أكتر من الموجود ({available}). عدّل حركات الصرف بالسعر ده الأول");
        }

        private async Task EnsureProjectNetIssuedNotNegativeAsync(int materialId, int? projectId)
        {
            if (projectId == null) return;

            var movements = await _unitOfWork.StockMovement
                .GetQueryable(m => m.MaterialId == materialId && m.ProjectId == projectId
                    && (m.MovementType == MovementType.IssueToProject || m.MovementType == MovementType.ReturnFromProject))
                .ToListAsync();

            var netIssued = movements.Where(m => m.MovementType == MovementType.IssueToProject).Sum(m => m.Quantity)
                - movements.Where(m => m.MovementType == MovementType.ReturnFromProject).Sum(m => m.Quantity);
            if (netIssued < 0)
                throw new Exception($"المرتجع من المشروع هيبقى أكبر من اللي اتصرفله فعلًا (الفرق {netIssued}) - عدّل أو احذف حركة المرتجع الأول");
        }

        private async Task AddProjectAuditAsync(int projectId, int employeeId, string description)
        {
            await _unitOfWork.ProjectAudit.AddAsync(new ProjectAudit
            {
                ProjectId = projectId,
                EmployeeId = employeeId,
                ActionDate = TimeHelper.NowInEgypt,
                ActionDescription = description
            });
        }

        private static string MovementLabel(MovementType type) => type switch
        {
            MovementType.Purchase => "شراء",
            MovementType.IssueToProject => "صرف",
            MovementType.ReturnFromProject => "مرتجع",
            MovementType.OpeningBalance => "رصيد افتتاحي",
            MovementType.Adjustment => "تسوية جرد",
            _ => type.ToString()
        };

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

            var newKeys = model.ExtraFieldDefinitions.Select(f => f.Key).ToList();
            if (newKeys.Count != newKeys.Distinct().Count())
                throw new Exception("في حقلين بنفس المفتاح - كل حقل لازم يكون ليه مفتاح مختلف");

            // الحقول اللي اتشالت من الفئة: قيمها بتتمسح من كل الأصناف التابعة ليها عشان متفضلش بيانات يتيمة
            var removedKeys = category.ExtraFieldDefinitions.Select(f => f.Key).Except(newKeys).ToList();
            if (removedKeys.Count > 0)
            {
                var materials = await _unitOfWork.Material.GetQueryable(m => m.CategoryId == category.Id).ToListAsync();
                foreach (var material in materials)
                {
                    var values = new Dictionary<string, string>(material.ExtraFieldValues);
                    foreach (var key in removedKeys) values.Remove(key);
                    material.ExtraFieldValues = values;
                    _unitOfWork.Material.Update(material);
                }
            }

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

            // الأصناف التابعة للنوع مش بتتمسح - بتتنقل لـ "بدون نوع" وقيم الخانات الإضافية بتاعتها بتتشال
            var materials = await _unitOfWork.Material.GetQueryable(m => m.CategoryId == id).ToListAsync();
            foreach (var material in materials)
            {
                material.CategoryId = null;
                material.ExtraFieldValues = new Dictionary<string, string>();
                _unitOfWork.Material.Update(material);
            }

            _unitOfWork.MaterialCategory.Delete(category);
            await _unitOfWork.CompleteAsync();
            return true;
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
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
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
                    TransportCost = model.TransportCost,
                    SupplierId = model.SupplierId,
                    MovementDate = TimeHelper.NowInEgypt,
                    Notes = model.Notes,
                    AttachmentUrl = model.AttachmentUrl,
                    AttachmentFileName = model.AttachmentFileName,
                    CreatedByEmployeeId = createdByEmployeeId
                };

                // النقل جزء من المستحق للمورد (الخامة + النقل = قيمة الشيك/التحويل اللي بيتكتبله)، فمش بيتخصم
                // من الخزنة هنا - بيتخصم مع دفعة المورد نفسها (كاش فورًا، أو الشيك لما يتصرف). بيتسجل على
                // الحركة بس عشان نعرف تكلفة الخامة الفعلية بالنقل
                await _unitOfWork.StockMovement.AddAsync(movement);
                await _unitOfWork.CompleteAsync();

                await transaction.CommitAsync();
                return await LoadMovementDTO(movement.Id);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<StockMovementDTO> UpdatePurchaseAsync(UpdatePurchaseDTO model, int employeeId)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var movement = await _unitOfWork.StockMovement.GetQueryable(m => m.Id == model.Id)
                    .Include(m => m.Material)
                    .FirstOrDefaultAsync() ?? throw new Exception("حركة الشراء غير موجودة");

                if (movement.MovementType != MovementType.Purchase)
                    throw new Exception("العملية دي مينفعش غير على حركات الشراء");

                var material = movement.Material;
                var quantityDelta = model.Quantity - movement.Quantity;
                if (material.Quantity + quantityDelta < 0)
                    throw new Exception($"لا يمكن تقليل الكمية - المتاح حاليًا من \"{material.Name}\" ({material.Quantity}) أقل من الفرق المطلوب خصمه");

                material.Quantity += quantityDelta;
                material.LastUpdated = TimeHelper.NowInEgypt;
                _unitOfWork.Material.Update(material);

                // لو تعديل خطأ في سعر آخر عملية شراء، لازم سعر الخامة الحالي يتحدث كمان
                var latestPurchase = await _unitOfWork.StockMovement
                    .GetQueryable(m => m.MaterialId == material.Id && m.MovementType == MovementType.Purchase)
                    .OrderByDescending(m => m.MovementDate).ThenByDescending(m => m.Id)
                    .FirstAsync();
                if (latestPurchase.Id == movement.Id)
                {
                    material.UnitPrice = model.UnitPrice;
                    _unitOfWork.Material.Update(material);
                }

                // بيانات قديمة: قبل كده مصروف النقل كان بيتخصم من الخزنة وقت الشراء - لو موجود بيتلغي
                // لأن النقل دلوقتي بيتدفع ضمن دفعة المورد
                await _cashBoxService.ReverseAsync(t => t.StockMovementId == movement.Id);

                movement.Quantity = model.Quantity;
                movement.UnitPriceAtTime = model.UnitPrice;
                movement.TransportCost = model.TransportCost;
                movement.SupplierId = model.SupplierId;
                movement.Notes = model.Notes;
                movement.AttachmentUrl = model.AttachmentUrl;
                movement.AttachmentFileName = model.AttachmentFileName;
                _unitOfWork.StockMovement.Update(movement);
                await _unitOfWork.CompleteAsync();

                await transaction.CommitAsync();
                return await LoadMovementDTO(movement.Id);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<List<StockPriceBatchDTO>> GetPriceBatchesAsync(int materialId)
        {
            var movements = await _unitOfWork.StockMovement.GetQueryable(m => m.MaterialId == materialId).ToListAsync();

            return movements
                .GroupBy(m => m.UnitPriceAtTime)
                .Select(g => new StockPriceBatchDTO
                {
                    UnitPrice = g.Key,
                    AvailableQuantity = g.Sum(SignedQuantity),
                    FirstPurchaseDate = g.Min(m => m.MovementDate)
                })
                .Where(b => b.AvailableQuantity > 0)
                .OrderBy(b => b.FirstPurchaseDate)
                .ToList();
        }

        public async Task<StockMovementDTO> IssueToProjectAsync(CreateIssueDTO model, int createdByEmployeeId)
        {
            var material = await _unitOfWork.Material.GetByIdAsync(model.MaterialId)
                ?? throw new Exception("الخامة غير موجودة");

            if (material.Quantity < model.Quantity)
                throw new Exception($"لا يوجد ما يكفي من الخامة \"{material.Name}\" في المخزن (المتاح: {material.Quantity} {material.Unit})");

            // بدل ما نستخدم السعر "الحالي" للخامة، لازم نتأكد إن السعر المطلوب الصرف بيه ده فعلاً
            // دفعة موجودة وكميتها كافية - عشان نسجل الصرف بالسعر التاريخي الصح لا بالأحدث بس
            var batches = await GetPriceBatchesAsync(model.MaterialId);
            var batch = batches.FirstOrDefault(b => b.UnitPrice == model.UnitPrice)
                ?? throw new Exception($"لا توجد دفعة متاحة بسعر {model.UnitPrice} لهذه الخامة");
            if (batch.AvailableQuantity < model.Quantity)
                throw new Exception($"الكمية المتاحة بسعر {model.UnitPrice} أقل من المطلوب (المتاح بهذا السعر: {batch.AvailableQuantity})");

            material.Quantity -= model.Quantity;
            material.LastUpdated = TimeHelper.NowInEgypt;
            _unitOfWork.Material.Update(material);

            var movement = new StockMovement
            {
                MaterialId = model.MaterialId,
                MovementType = MovementType.IssueToProject,
                Quantity = model.Quantity,
                UnitPriceAtTime = model.UnitPrice,
                ProjectId = model.ProjectId,
                MovementDate = TimeHelper.NowInEgypt,
                Notes = model.Notes,
                AttachmentUrl = model.AttachmentUrl,
                AttachmentFileName = model.AttachmentFileName,
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
            TransportCost = m.TransportCost,
            ProjectId = m.ProjectId,
            ProjectCode = m.Project?.ProjectCode,
            ProjectName = m.Project?.Name,
            SupplierId = m.SupplierId,
            SupplierName = m.Supplier?.Name,
            MovementDate = m.MovementDate,
            Notes = m.Notes,
            CreatedByEmployeeName = m.CreatedByEmployee.Name,
            AttachmentUrl = m.AttachmentUrl,
            AttachmentFileName = m.AttachmentFileName
        };
    }
}
