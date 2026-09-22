using KMG.Core.DTOs.AiContext;
using KMG.Core.Helper;
using KMG.Core.Interfaces;
using KMG.Core.Interfaces.Services;
using KMG.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace KMG.Core.Services
{
    public class AiContextService : IAiContextService
    {
        private readonly IUnitOfWork _unitOfWork;
        private const int MaxSampleMaterialsPerCategory = 10;

        public AiContextService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        private async Task<AiPromptConfig> GetOrCreatePromptConfigAsync()
        {
            var config = (await _unitOfWork.AiPromptConfig.GetAllAsync()).FirstOrDefault();
            if (config == null)
            {
                config = new AiPromptConfig { PromptText = string.Empty, UpdatedAt = TimeHelper.NowInEgypt };
                await _unitOfWork.AiPromptConfig.AddAsync(config);
                await _unitOfWork.CompleteAsync();
            }
            return config;
        }

        public async Task<AiPromptDTO> GetPromptAsync()
        {
            var config = await GetOrCreatePromptConfigAsync();
            var withEmployee = await _unitOfWork.AiPromptConfig.GetQueryable(c => c.Id == config.Id)
                .Include(c => c.UpdatedByEmployee)
                .FirstAsync();

            return MapPrompt(withEmployee);
        }

        public async Task<AiPromptDTO> UpdatePromptAsync(string promptText, int employeeId)
        {
            var config = await GetOrCreatePromptConfigAsync();
            config.PromptText = promptText ?? string.Empty;
            config.UpdatedAt = TimeHelper.NowInEgypt;
            config.UpdatedByEmployeeId = employeeId;
            _unitOfWork.AiPromptConfig.Update(config);
            await _unitOfWork.CompleteAsync();

            var withEmployee = await _unitOfWork.AiPromptConfig.GetQueryable(c => c.Id == config.Id)
                .Include(c => c.UpdatedByEmployee)
                .FirstAsync();

            return MapPrompt(withEmployee);
        }

        private static AiPromptDTO MapPrompt(AiPromptConfig config) => new()
        {
            PromptText = config.PromptText,
            UpdatedAt = config.UpdatedAt,
            UpdatedByEmployeeName = config.UpdatedByEmployee?.Name
        };

        public async Task<AiContextResponseDTO> GetContextDataAsync()
        {
            var promptConfig = await GetOrCreatePromptConfigAsync();

            var projects = await _unitOfWork.Project.GetQueryable(null)
                .Select(p => new AiProjectSummaryDTO
                {
                    Name = p.Name,
                    ProjectType = p.ProjectType.ToString(),
                    Status = p.Status.ToString(),
                    Description = p.Description
                })
                .ToListAsync();

            var projectsByType = projects
                .GroupBy(p => p.ProjectType)
                .ToDictionary(g => g.Key, g => g.Count());

            var projectsByStatus = projects
                .GroupBy(p => p.Status)
                .ToDictionary(g => g.Key, g => g.Count());

            var totalClients = await _unitOfWork.Client.GetQueryable(null).CountAsync();
            var totalSuppliers = await _unitOfWork.Supplier.GetQueryable(null).CountAsync();
            var totalMaterialTypes = await _unitOfWork.Material.GetQueryable(null).CountAsync();

            var categories = await _unitOfWork.MaterialCategory.GetQueryable(null)
                .Include(c => c.Materials)
                .ToListAsync();

            var materialCategories = categories
                .Select(c => new AiMaterialCategorySummaryDTO
                {
                    CategoryName = c.Name,
                    ItemCount = c.Materials.Count,
                    SampleMaterialNames = c.Materials
                        .Select(m => m.Name)
                        .Distinct()
                        .Take(MaxSampleMaterialsPerCategory)
                        .ToList()
                })
                .ToList();

            var uncategorizedMaterials = await _unitOfWork.Material.GetQueryable(m => m.CategoryId == null)
                .ToListAsync();
            if (uncategorizedMaterials.Count > 0)
            {
                materialCategories.Add(new AiMaterialCategorySummaryDTO
                {
                    CategoryName = "بدون نوع",
                    ItemCount = uncategorizedMaterials.Count,
                    SampleMaterialNames = uncategorizedMaterials
                        .Select(m => m.Name)
                        .Distinct()
                        .Take(MaxSampleMaterialsPerCategory)
                        .ToList()
                });
            }

            return new AiContextResponseDTO
            {
                GeneratedAt = TimeHelper.NowInEgypt,
                DynamicPrompt = promptConfig.PromptText,
                CompanyOverview = new AiCompanyOverviewDTO
                {
                    TotalProjects = projects.Count,
                    ProjectsByType = projectsByType,
                    ProjectsByStatus = projectsByStatus,
                    TotalClients = totalClients,
                    TotalSuppliers = totalSuppliers,
                    TotalMaterialTypes = totalMaterialTypes
                },
                Projects = projects,
                MaterialCategories = materialCategories
            };
        }

        public async Task<AiTenderResultDTO> IngestTenderResultAsync(CreateAiTenderResultDTO model)
        {
            var now = TimeHelper.NowInEgypt;
            var existing = await UpsertTenderResultAsync(model, now);
            await _unitOfWork.CompleteAsync();
            return MapTenderResult(existing);
        }

        public async Task<List<AiTenderResultDTO>> IngestTenderResultsAsync(List<CreateAiTenderResultDTO> matches)
        {
            var now = TimeHelper.NowInEgypt;
            var saved = new List<AiTenderResult>();
            foreach (var model in matches)
                saved.Add(await UpsertTenderResultAsync(model, now));

            await _unitOfWork.CompleteAsync();
            return saved.Select(MapTenderResult).ToList();
        }

        // منطق الإضافة/التحديث المشترك بين استقبال مناقصة واحدة ودفعة كاملة - بيضيف/يعدّل الكيان
        // في الـ context بس من غير CompleteAsync() عشان المنادي يقدر يجمّع كذا عملية في معاملة واحدة
        private async Task<AiTenderResult> UpsertTenderResultAsync(CreateAiTenderResultDTO model, DateTime now)
        {
            var existing = await _unitOfWork.AiTenderResult.GetQueryable(t => t.TenderId == model.TenderId).FirstOrDefaultAsync();
            var isNew = existing == null;

            if (existing == null)
                existing = new AiTenderResult { TenderId = model.TenderId, FirstReceivedAt = now };

            existing.TenderTitle = model.TenderTitle;
            existing.IssuingEntity = model.IssuingEntity;
            existing.SourceSite = model.SourceSite;
            existing.SourceUrl = model.SourceUrl;
            existing.SubmissionDeadline = model.SubmissionDeadline;
            existing.DaysUntilDeadline = model.DaysUntilDeadline;
            existing.BusinessCategory = model.BusinessCategory;
            existing.IsNewCategory = model.IsNewCategory;
            existing.MatchedVia = model.MatchedVia;
            existing.MatchReason = model.MatchReason;
            existing.DocumentReadStatus = model.DocumentReadStatus;
            existing.DocumentEntity = model.DocumentEntity;
            existing.ScopeOfWork = model.ScopeOfWork;
            existing.MaterialsRequired = model.MaterialsRequired;
            existing.Quantities = model.Quantities;
            existing.Location = model.Location;
            existing.BookletFee = model.BookletFee;
            existing.InitialInsurance = model.InitialInsurance;
            existing.DocumentSubmitBy = model.DocumentSubmitBy;
            existing.ContactInfo = model.ContactInfo;
            existing.AiDocumentRelevant = model.AiDocumentRelevant;
            existing.RelevanceNote = model.RelevanceNote;
            existing.LastUpdatedAt = now;
            // لو المستخدم قال "مش مهتم" قبل كده، تحديث جديد من الـ AI لنفس المناقصة ميرجعهاش تاني
            // (القرار ده متعمد وثابت، مش هيتغير غصب عن المستخدم)

            if (isNew)
                await _unitOfWork.AiTenderResult.AddAsync(existing);
            else
                _unitOfWork.AiTenderResult.Update(existing);

            return existing;
        }

        public async Task<List<AiTenderResultDTO>> GetActiveTenderResultsAsync()
        {
            var results = await _unitOfWork.AiTenderResult.GetQueryable(t => !t.IsDismissed)
                .OrderByDescending(t => t.LastUpdatedAt)
                .ToListAsync();

            return results.Select(MapTenderResult).ToList();
        }

        public async Task<AiTenderResultDTO?> GetTenderResultByIdAsync(int id)
        {
            var result = await _unitOfWork.AiTenderResult.GetByIdAsync(id);
            return result == null ? null : MapTenderResult(result);
        }

        public async Task DismissTenderResultAsync(int id)
        {
            var result = await _unitOfWork.AiTenderResult.GetByIdAsync(id);
            if (result == null) return;

            result.IsDismissed = true;
            _unitOfWork.AiTenderResult.Update(result);
            await _unitOfWork.CompleteAsync();
        }

        private static AiTenderResultDTO MapTenderResult(AiTenderResult t) => new()
        {
            Id = t.Id,
            TenderId = t.TenderId,
            TenderTitle = t.TenderTitle,
            IssuingEntity = t.IssuingEntity,
            SourceSite = t.SourceSite,
            SourceUrl = t.SourceUrl,
            SubmissionDeadline = t.SubmissionDeadline,
            DaysUntilDeadline = t.DaysUntilDeadline,
            BusinessCategory = t.BusinessCategory,
            IsNewCategory = t.IsNewCategory,
            MatchedVia = t.MatchedVia,
            MatchReason = t.MatchReason,
            DocumentReadStatus = t.DocumentReadStatus,
            DocumentEntity = t.DocumentEntity,
            ScopeOfWork = t.ScopeOfWork,
            MaterialsRequired = t.MaterialsRequired,
            Quantities = t.Quantities,
            Location = t.Location,
            BookletFee = t.BookletFee,
            InitialInsurance = t.InitialInsurance,
            DocumentSubmitBy = t.DocumentSubmitBy,
            ContactInfo = t.ContactInfo,
            AiDocumentRelevant = t.AiDocumentRelevant,
            RelevanceNote = t.RelevanceNote,
            FirstReceivedAt = t.FirstReceivedAt,
            LastUpdatedAt = t.LastUpdatedAt
        };
    }
}
