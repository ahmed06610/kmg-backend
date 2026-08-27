using KMG.Core.DTOs.Project;
using KMG.Core.DTOs.Stock;
using KMG.Core.Enums;
using KMG.Core.Helper;
using KMG.Core.Interfaces;
using KMG.Core.Interfaces.Services;
using KMG.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace KMG.Core.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICashBoxService _cashBoxService;

        public ProjectService(IUnitOfWork unitOfWork, ICashBoxService cashBoxService)
        {
            _unitOfWork = unitOfWork;
            _cashBoxService = cashBoxService;
        }

        private static IQueryable<Project> IncludeForFinancials(IQueryable<Project> query) =>
            query.Include(p => p.Client)
                 .Include(p => p.Payments)
                 .Include(p => p.Expenses)
                 .Include(p => p.StockMovements)
                 .Include(p => p.Missions).ThenInclude(m => m.MissionWorkers).ThenInclude(w => w.Employee);

        public async Task<List<ProjectListDTO>> GetAllAsync()
        {
            var projects = await IncludeForFinancials(_unitOfWork.Project.GetQueryable(null))
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return projects.Select(MapList).ToList();
        }

        public async Task<ProjectDetailsDTO?> GetByIdAsync(int id)
        {
            var project = await IncludeForFinancials(_unitOfWork.Project.GetQueryable(p => p.Id == id))
                .Include(p => p.StockMovements).ThenInclude(m => m.Material)
                .Include(p => p.StockMovements).ThenInclude(m => m.CreatedByEmployee)
                .Include(p => p.Attachments).ThenInclude(a => a.UploadedByEmployee)
                .Include(p => p.AuditLogs).ThenInclude(a => a.Employee)
                .FirstOrDefaultAsync();

            if (project == null) return null;

            var list = MapList(project);
            return new ProjectDetailsDTO
            {
                Id = list.Id,
                ProjectCode = list.ProjectCode,
                ProjectType = list.ProjectType,
                Status = list.Status,
                ClientName = list.ClientName,
                ContractValue = list.ContractValue,
                TotalCollected = list.TotalCollected,
                RemainingBalance = list.RemainingBalance,
                NetProfit = list.NetProfit,
                CreatedAt = list.CreatedAt,
                ClientId = project.ClientId,
                Description = project.Description,
                TenderInsuranceAmount = project.TenderInsuranceAmount,
                TenderTaxAmount = project.TenderTaxAmount,
                SupplyProfitMargin = project.SupplyProfitMargin,
                TotalMaterialsCost = project.TotalMaterialsCost,
                TotalPettyExpenses = project.TotalPettyExpenses,
                TotalLaborCost = project.TotalLaborCost,
                Payments = project.Payments.OrderByDescending(p => p.PaymentDate).Select(p => new ProjectPaymentDTO
                {
                    Id = p.Id,
                    Amount = p.Amount,
                    AmountCash = p.AmountCash,
                    AmountCredit = p.AmountCredit,
                    PaymentDate = p.PaymentDate,
                    Notes = p.Notes
                }).ToList(),
                Expenses = project.Expenses.OrderByDescending(e => e.ExpenseDate).Select(e => new ProjectExpenseDTO
                {
                    Id = e.Id,
                    Amount = e.Amount,
                    Category = e.Category.ToString(),
                    Description = e.Description,
                    ExpenseDate = e.ExpenseDate,
                    AttachmentUrl = e.AttachmentUrl
                }).ToList(),
                StockMovements = project.StockMovements.OrderByDescending(m => m.MovementDate).Select(m => new StockMovementDTO
                {
                    Id = m.Id,
                    MovementType = m.MovementType.ToString(),
                    MaterialId = m.MaterialId,
                    MaterialName = m.Material.Name,
                    Quantity = m.Quantity,
                    UnitPriceAtTime = m.UnitPriceAtTime,
                    ProjectId = m.ProjectId,
                    ProjectCode = project.ProjectCode,
                    MovementDate = m.MovementDate,
                    Notes = m.Notes,
                    CreatedByEmployeeName = m.CreatedByEmployee.Name
                }).ToList(),
                Attachments = project.Attachments.OrderByDescending(a => a.UploadedAt).Select(a => new ProjectAttachmentDTO
                {
                    Id = a.Id,
                    FileUrl = a.FileUrl,
                    FileName = a.FileName,
                    Description = a.Description,
                    UploadedAt = a.UploadedAt,
                    UploadedByEmployeeName = a.UploadedByEmployee.Name
                }).ToList(),
                AuditLogs = project.AuditLogs.OrderByDescending(a => a.ActionDate).Select(a => new ProjectAuditDTO
                {
                    Id = a.Id,
                    ActionDescription = a.ActionDescription,
                    ActionDate = a.ActionDate,
                    EmployeeName = a.Employee.Name
                }).ToList()
            };
        }

        public async Task<int> CreateAsync(CreateProjectDTO model, int createdByEmployeeId)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var projectCode = await GenerateProjectCodeAsync();

                var project = new Project
                {
                    ProjectCode = projectCode,
                    ProjectType = model.ProjectType,
                    Status = ProjectStatus.New,
                    ClientId = model.ClientId,
                    ContractValue = model.ContractValue,
                    Description = model.Description,
                    TenderInsuranceAmount = model.TenderInsuranceAmount,
                    TenderTaxAmount = model.TenderTaxAmount,
                    SupplyProfitMargin = model.SupplyProfitMargin,
                    CreatedByEmployeeId = createdByEmployeeId,
                    CreatedAt = TimeHelper.NowInEgypt
                };

                await _unitOfWork.Project.AddAsync(project);
                await _unitOfWork.CompleteAsync(); // نحتاج project.Id لو هننشئ مصاريف التأمين/الضريبة تحته

                await _unitOfWork.ProjectAudit.AddAsync(new ProjectAudit
                {
                    ProjectId = project.Id,
                    EmployeeId = createdByEmployeeId,
                    ActionDate = TimeHelper.NowInEgypt,
                    ActionDescription = "تم إنشاء المشروع"
                });

                // تأمين وضريبة المناقصة (لو موجودين) بيتسجلوا كمصروف نثري فعلي وحركة خزنة صادرة،
                // مش مجرد حقول مخزّنة، عشان يدخلوا في صافي ربح المشروع ورصيد الخزنة فعليًا
                if (model.TenderInsuranceAmount is > 0)
                    await RecordTenderCostAsync(project, model.TenderInsuranceAmount.Value, ExpenseCategory.TenderInsurance, "تأمين المناقصة", createdByEmployeeId);

                if (model.TenderTaxAmount is > 0)
                    await RecordTenderCostAsync(project, model.TenderTaxAmount.Value, ExpenseCategory.TenderTax, "ضريبة المناقصة", createdByEmployeeId);

                await _unitOfWork.CompleteAsync();
                await transaction.CommitAsync();

                return project.Id;
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        private async Task RecordTenderCostAsync(Project project, decimal amount, ExpenseCategory category, string description, int createdByEmployeeId)
        {
            var expense = new ProjectExpense
            {
                ProjectId = project.Id,
                Amount = amount,
                Category = category,
                Description = description,
                ExpenseDate = project.CreatedAt,
                CreatedByEmployeeId = createdByEmployeeId
            };

            await _unitOfWork.ProjectExpense.AddAsync(expense);
            await _unitOfWork.CompleteAsync(); // نحتاج expense.Id عشان نربط بيه حركة الخزنة

            await _cashBoxService.RecordTransactionAsync(
                amountCash: -amount,
                amountCredit: 0,
                type: TransactionType.ProjectExpenseOut,
                description: $"{description} - مشروع {project.ProjectCode}",
                createdByEmployeeId: createdByEmployeeId,
                projectId: project.Id,
                projectExpenseId: expense.Id);
        }

        public async Task<bool> UpdateStatusAsync(UpdateProjectStatusDTO model, int employeeId)
        {
            var project = await _unitOfWork.Project.GetByIdAsync(model.ProjectId);
            if (project == null) return false;

            project.Status = model.Status;
            _unitOfWork.Project.Update(project);

            await _unitOfWork.ProjectAudit.AddAsync(new ProjectAudit
            {
                ProjectId = project.Id,
                EmployeeId = employeeId,
                ActionDate = TimeHelper.NowInEgypt,
                ActionDescription = $"تم تغيير حالة المشروع إلى: {model.Status}"
            });

            await _unitOfWork.CompleteAsync();
            return true;
        }

        public async Task<ProjectPaymentDTO> RecordPaymentAsync(CreateProjectPaymentDTO model, int createdByEmployeeId)
        {
            if (model.AmountCash < 0 || model.AmountCredit < 0)
                throw new Exception("لا يمكن أن تكون قيمة الكاش أو الكريديت سالبة");
            if (model.AmountCash + model.AmountCredit <= 0)
                throw new Exception("قيمة الدفعة يجب أن تكون أكبر من صفر");

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var project = await _unitOfWork.Project.GetQueryable(p => p.Id == model.ProjectId)
                    .Include(p => p.Payments)
                    .FirstOrDefaultAsync()
                    ?? throw new Exception("المشروع غير موجود");

                if (model.AmountCash + model.AmountCredit > project.RemainingBalance)
                    throw new Exception($"قيمة الدفعة أكبر من المتبقي على المشروع (المتبقي: {project.RemainingBalance})");

                var payment = new ProjectPayment
                {
                    ProjectId = model.ProjectId,
                    Amount = model.AmountCash + model.AmountCredit,
                    AmountCash = model.AmountCash,
                    AmountCredit = model.AmountCredit,
                    PaymentDate = model.PaymentDate,
                    Notes = model.Notes,
                    CreatedByEmployeeId = createdByEmployeeId
                };

                await _unitOfWork.ProjectPayment.AddAsync(payment);

                await _cashBoxService.RecordTransactionAsync(
                    amountCash: model.AmountCash,
                    amountCredit: model.AmountCredit,
                    type: TransactionType.ProjectPaymentIn,
                    description: $"تحصيل دفعة من مشروع: {project.ProjectCode}",
                    createdByEmployeeId: createdByEmployeeId,
                    projectId: model.ProjectId);

                await _unitOfWork.ProjectAudit.AddAsync(new ProjectAudit
                {
                    ProjectId = model.ProjectId,
                    EmployeeId = createdByEmployeeId,
                    ActionDate = TimeHelper.NowInEgypt,
                    ActionDescription = $"تحصيل دفعة بقيمة {payment.Amount}"
                });

                await _unitOfWork.CompleteAsync();
                await transaction.CommitAsync();

                return new ProjectPaymentDTO
                {
                    Id = payment.Id,
                    Amount = payment.Amount,
                    AmountCash = payment.AmountCash,
                    AmountCredit = payment.AmountCredit,
                    PaymentDate = payment.PaymentDate,
                    Notes = payment.Notes
                };
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<ProjectExpenseDTO> RecordExpenseAsync(CreateProjectExpenseDTO model, int createdByEmployeeId)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var project = await _unitOfWork.Project.GetByIdAsync(model.ProjectId)
                    ?? throw new Exception("المشروع غير موجود");

                var expense = new ProjectExpense
                {
                    ProjectId = model.ProjectId,
                    Amount = model.Amount,
                    Category = model.Category,
                    Description = model.Description,
                    ExpenseDate = model.ExpenseDate,
                    AttachmentUrl = model.AttachmentUrl,
                    CreatedByEmployeeId = createdByEmployeeId
                };

                await _unitOfWork.ProjectExpense.AddAsync(expense);
                await _unitOfWork.CompleteAsync(); // نحتاج Id بتاع المصروف الأول

                if (model.PaidFromCashBox)
                {
                    await _cashBoxService.RecordTransactionAsync(
                        amountCash: -model.Amount,
                        amountCredit: 0,
                        type: TransactionType.ProjectExpenseOut,
                        description: $"مصروف نثري لمشروع {project.ProjectCode}: {model.Description}",
                        createdByEmployeeId: createdByEmployeeId,
                        projectId: model.ProjectId,
                        projectExpenseId: expense.Id);
                }

                await _unitOfWork.ProjectAudit.AddAsync(new ProjectAudit
                {
                    ProjectId = model.ProjectId,
                    EmployeeId = createdByEmployeeId,
                    ActionDate = TimeHelper.NowInEgypt,
                    ActionDescription = $"تسجيل مصروف نثري بقيمة {model.Amount} ({model.Category})"
                });

                await _unitOfWork.CompleteAsync();
                await transaction.CommitAsync();

                return new ProjectExpenseDTO
                {
                    Id = expense.Id,
                    Amount = expense.Amount,
                    Category = expense.Category.ToString(),
                    Description = expense.Description,
                    ExpenseDate = expense.ExpenseDate,
                    AttachmentUrl = expense.AttachmentUrl
                };
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<ProjectAttachmentDTO> AddAttachmentAsync(CreateProjectAttachmentDTO model, int uploadedByEmployeeId)
        {
            var attachment = new ProjectAttachment
            {
                ProjectId = model.ProjectId,
                FileUrl = model.FileUrl,
                FileName = model.FileName,
                Description = model.Description,
                UploadedAt = TimeHelper.NowInEgypt,
                UploadedByEmployeeId = uploadedByEmployeeId
            };

            await _unitOfWork.ProjectAttachment.AddAsync(attachment);
            await _unitOfWork.CompleteAsync();

            var employee = await _unitOfWork.Employee.GetQueryable(e => e.Id == uploadedByEmployeeId)
                .FirstAsync();

            return new ProjectAttachmentDTO
            {
                Id = attachment.Id,
                FileUrl = attachment.FileUrl,
                FileName = attachment.FileName,
                Description = attachment.Description,
                UploadedAt = attachment.UploadedAt,
                UploadedByEmployeeName = employee.Name
            };
        }

        private async Task<string> GenerateProjectCodeAsync()
        {
            var year = TimeHelper.NowInEgypt.Year;
            var countThisYear = (await _unitOfWork.Project.FindAllAsync(p => p.CreatedAt.Year == year)).Count();
            return $"KMG-{year}-{(countThisYear + 1):D4}";
        }

        private static ProjectListDTO MapList(Project p) => new()
        {
            Id = p.Id,
            ProjectCode = p.ProjectCode,
            ProjectType = p.ProjectType.ToString(),
            Status = p.Status.ToString(),
            ClientName = p.Client.Name,
            ContractValue = p.ContractValue,
            TotalCollected = p.TotalCollected,
            RemainingBalance = p.RemainingBalance,
            NetProfit = p.NetProfit,
            CreatedAt = p.CreatedAt
        };
    }
}
