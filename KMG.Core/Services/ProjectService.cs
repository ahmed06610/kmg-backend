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
                 .Include(p => p.WriteOffs)
                 .Include(p => p.Missions).ThenInclude(m => m.MissionWorkers).ThenInclude(w => w.Employee);

        public async Task<List<ProjectListDTO>> GetAllAsync()
        {
            // AsSplitQuery() ضروري هنا: من غير كده EF بيعمل LEFT JOIN واحد بين كل الكولكشنز
            // (Payments × Expenses × StockMovements × Missions×Workers) في نفس الوقت، فعدد الصفوف
            // بيتضاعف (cartesian explosion) وده كان بيخلي الاستعلام ده ياخد 25+ ثانية مع بيانات حقيقية
            var projects = await IncludeForFinancials(_unitOfWork.Project.GetQueryable(null))
                .OrderByDescending(p => p.CreatedAt)
                .AsSplitQuery()
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
                .AsSplitQuery()
                .FirstOrDefaultAsync();

            if (project == null) return null;

            var list = MapList(project);
            return new ProjectDetailsDTO
            {
                Id = list.Id,
                Name = list.Name,
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
                ClientPhone = project.Client.Phone,
                ClientEmail = project.Client.Email,
                ClientAddress = project.Client.Address,
                Description = project.Description,
                TenderInsuranceAmount = project.TenderInsuranceAmount,
                TenderTaxAmount = project.TenderTaxAmount,
                SupplyProfitMargin = project.SupplyProfitMargin,
                TotalMaterialsCost = project.TotalMaterialsCost,
                TotalPettyExpenses = project.TotalPettyExpenses,
                TotalLaborCost = project.TotalLaborCost,
                TotalWriteOffs = project.TotalWriteOffs,
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
                }).ToList(),
                WriteOffs = project.WriteOffs.OrderByDescending(w => w.WriteOffDate).Select(w => new ProjectWriteOffDTO
                {
                    Id = w.Id,
                    Amount = w.Amount,
                    Reason = w.Reason,
                    WriteOffDate = w.WriteOffDate
                }).ToList(),
                CanDelete = !project.Payments.Any() && !project.Expenses.Any() && !project.Missions.Any()
                    && !project.StockMovements.Any() && !project.Attachments.Any() && !project.WriteOffs.Any()
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
                    Name = model.Name,
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

        public async Task<bool> UpdateAsync(UpdateProjectDTO model, int employeeId)
        {
            var project = await _unitOfWork.Project.GetByIdAsync(model.Id);
            if (project == null) return false;

            project.Name = model.Name;
            project.Description = model.Description;
            project.ClientId = model.ClientId;
            project.ContractValue = model.ContractValue;
            _unitOfWork.Project.Update(project);

            await _unitOfWork.ProjectAudit.AddAsync(new ProjectAudit
            {
                ProjectId = project.Id,
                EmployeeId = employeeId,
                ActionDate = TimeHelper.NowInEgypt,
                ActionDescription = "تم تعديل بيانات المشروع"
            });

            await _unitOfWork.CompleteAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var project = await _unitOfWork.Project.GetQueryable(p => p.Id == id)
                .Include(p => p.Payments)
                .Include(p => p.Expenses)
                .Include(p => p.Missions)
                .Include(p => p.StockMovements)
                .Include(p => p.Attachments)
                .Include(p => p.WriteOffs)
                .AsSplitQuery()
                .FirstOrDefaultAsync();
            if (project == null) return false;

            if (project.Payments.Any() || project.Expenses.Any() || project.Missions.Any()
                || project.StockMovements.Any() || project.Attachments.Any() || project.WriteOffs.Any())
                throw new Exception("لا يمكن حذف مشروع له بيانات مسجلة (دفعات/مصاريف/مأموريات/حركات مخزون/مرفقات/خصومات أعمال) - احذفها أولًا");

            _unitOfWork.Project.Delete(project);
            await _unitOfWork.CompleteAsync();
            return true;
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
                ActionDescription = $"تم تغيير حالة المشروع إلى: {model.Status.Arabic()}"
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
                await _unitOfWork.CompleteAsync(); // نحتاج payment.Id عشان نربط بيه حركة الخزنة

                await _cashBoxService.RecordTransactionAsync(
                    amountCash: model.AmountCash,
                    amountCredit: model.AmountCredit,
                    type: TransactionType.ProjectPaymentIn,
                    description: $"تحصيل دفعة من مشروع: {project.ProjectCode}",
                    createdByEmployeeId: createdByEmployeeId,
                    projectId: model.ProjectId,
                    projectPaymentId: payment.Id);

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

        public async Task<ProjectPaymentDTO> UpdatePaymentAsync(UpdateProjectPaymentDTO model, int employeeId)
        {
            if (model.AmountCash < 0 || model.AmountCredit < 0)
                throw new Exception("لا يمكن أن تكون قيمة الكاش أو الكريديت سالبة");
            var newAmount = model.AmountCash + model.AmountCredit;
            if (newAmount <= 0)
                throw new Exception("قيمة الدفعة يجب أن تكون أكبر من صفر");

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var payment = await _unitOfWork.ProjectPayment.GetQueryable(p => p.Id == model.Id)
                    .Include(p => p.Project).ThenInclude(pr => pr.Payments)
                    .FirstOrDefaultAsync() ?? throw new Exception("الدفعة غير موجودة");

                var project = payment.Project;
                var otherPaymentsTotal = project.Payments.Where(p => p.Id != payment.Id).Sum(p => p.Amount);
                if (otherPaymentsTotal + newAmount > project.ContractValue)
                    throw new Exception($"قيمة الدفعة أكبر من المتبقي على المشروع (المتبقي: {project.ContractValue - otherPaymentsTotal})");

                await _cashBoxService.ReverseAsync(t => t.ProjectPaymentId == payment.Id);

                payment.AmountCash = model.AmountCash;
                payment.AmountCredit = model.AmountCredit;
                payment.Amount = newAmount;
                payment.PaymentDate = model.PaymentDate;
                payment.Notes = model.Notes;
                _unitOfWork.ProjectPayment.Update(payment);

                await _cashBoxService.RecordTransactionAsync(
                    amountCash: payment.AmountCash,
                    amountCredit: payment.AmountCredit,
                    type: TransactionType.ProjectPaymentIn,
                    description: $"تعديل دفعة من مشروع: {project.ProjectCode}",
                    createdByEmployeeId: employeeId,
                    projectId: project.Id,
                    projectPaymentId: payment.Id);

                await _unitOfWork.ProjectAudit.AddAsync(new ProjectAudit
                {
                    ProjectId = project.Id,
                    EmployeeId = employeeId,
                    ActionDate = TimeHelper.NowInEgypt,
                    ActionDescription = $"تعديل دفعة لتصبح بقيمة {payment.Amount}"
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

        public async Task<bool> DeletePaymentAsync(int id, int employeeId)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var payment = await _unitOfWork.ProjectPayment.GetQueryable(p => p.Id == id)
                    .Include(p => p.Project)
                    .FirstOrDefaultAsync();
                if (payment == null) return false;

                await _cashBoxService.ReverseAsync(t => t.ProjectPaymentId == id);
                _unitOfWork.ProjectPayment.Delete(payment);

                await _unitOfWork.ProjectAudit.AddAsync(new ProjectAudit
                {
                    ProjectId = payment.ProjectId,
                    EmployeeId = employeeId,
                    ActionDate = TimeHelper.NowInEgypt,
                    ActionDescription = $"حذف دفعة كانت بقيمة {payment.Amount}"
                });

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
                    ActionDescription = $"تسجيل مصروف نثري بقيمة {model.Amount} ({model.Category.Arabic()})"
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

        public async Task<ProjectExpenseDTO> UpdateExpenseAsync(UpdateProjectExpenseDTO model, int employeeId)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var expense = await _unitOfWork.ProjectExpense.GetQueryable(e => e.Id == model.Id)
                    .Include(e => e.Project)
                    .FirstOrDefaultAsync() ?? throw new Exception("المصروف غير موجود");

                var hadCashBoxEffect = (await _unitOfWork.CashBoxTransaction.GetQueryable(t => t.ProjectExpenseId == expense.Id).AnyAsync());
                if (hadCashBoxEffect)
                    await _cashBoxService.ReverseAsync(t => t.ProjectExpenseId == expense.Id);

                expense.Amount = model.Amount;
                expense.Category = model.Category;
                expense.Description = model.Description;
                expense.ExpenseDate = model.ExpenseDate;
                _unitOfWork.ProjectExpense.Update(expense);

                if (hadCashBoxEffect)
                {
                    await _cashBoxService.RecordTransactionAsync(
                        amountCash: -model.Amount,
                        amountCredit: 0,
                        type: TransactionType.ProjectExpenseOut,
                        description: $"تعديل مصروف نثري لمشروع {expense.Project.ProjectCode}: {model.Description}",
                        createdByEmployeeId: employeeId,
                        projectId: expense.ProjectId,
                        projectExpenseId: expense.Id);
                }

                await _unitOfWork.ProjectAudit.AddAsync(new ProjectAudit
                {
                    ProjectId = expense.ProjectId,
                    EmployeeId = employeeId,
                    ActionDate = TimeHelper.NowInEgypt,
                    ActionDescription = $"تعديل مصروف نثري ليصبح بقيمة {model.Amount}"
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

        public async Task<bool> DeleteExpenseAsync(int id, int employeeId)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var expense = await _unitOfWork.ProjectExpense.GetByIdAsync(id);
                if (expense == null) return false;

                await _cashBoxService.ReverseAsync(t => t.ProjectExpenseId == id);
                _unitOfWork.ProjectExpense.Delete(expense);

                await _unitOfWork.ProjectAudit.AddAsync(new ProjectAudit
                {
                    ProjectId = expense.ProjectId,
                    EmployeeId = employeeId,
                    ActionDate = TimeHelper.NowInEgypt,
                    ActionDescription = $"حذف مصروف نثري كان بقيمة {expense.Amount}"
                });

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

        public async Task<bool> UpdateAttachmentAsync(UpdateProjectAttachmentDTO model)
        {
            var attachment = await _unitOfWork.ProjectAttachment.GetByIdAsync(model.Id);
            if (attachment == null) return false;

            attachment.Description = model.Description;
            _unitOfWork.ProjectAttachment.Update(attachment);
            await _unitOfWork.CompleteAsync();
            return true;
        }

        /// <returns>رابط الملف المحذوف عشان الـ Controller يقدر يمسحه فعليًا من التخزين، أو null لو المرفق مش موجود</returns>
        public async Task<string?> DeleteAttachmentAsync(int id)
        {
            var attachment = await _unitOfWork.ProjectAttachment.GetByIdAsync(id);
            if (attachment == null) return null;

            _unitOfWork.ProjectAttachment.Delete(attachment);
            await _unitOfWork.CompleteAsync();
            return attachment.FileUrl;
        }

        public async Task<ProjectWriteOffDTO> CreateWriteOffAsync(CreateProjectWriteOffDTO model, int employeeId)
        {
            if (model.Amount <= 0)
                throw new Exception("قيمة الخصم يجب أن تكون أكبر من صفر");

            var project = await _unitOfWork.Project.GetQueryable(p => p.Id == model.ProjectId)
                .Include(p => p.Payments)
                .Include(p => p.WriteOffs)
                .FirstOrDefaultAsync() ?? throw new Exception("المشروع غير موجود");

            if (model.Amount > project.RemainingBalance)
                throw new Exception($"قيمة الخصم أكبر من المتبقي على المشروع (المتبقي: {project.RemainingBalance})");

            var writeOff = new ProjectWriteOff
            {
                ProjectId = model.ProjectId,
                Amount = model.Amount,
                Reason = model.Reason,
                WriteOffDate = model.WriteOffDate,
                CreatedByEmployeeId = employeeId
            };

            await _unitOfWork.ProjectWriteOff.AddAsync(writeOff);

            await _unitOfWork.ProjectAudit.AddAsync(new ProjectAudit
            {
                ProjectId = model.ProjectId,
                EmployeeId = employeeId,
                ActionDate = TimeHelper.NowInEgypt,
                ActionDescription = $"تسجيل خصم أعمال بقيمة {model.Amount}: {model.Reason}"
            });

            await _unitOfWork.CompleteAsync();

            return new ProjectWriteOffDTO { Id = writeOff.Id, Amount = writeOff.Amount, Reason = writeOff.Reason, WriteOffDate = writeOff.WriteOffDate };
        }

        public async Task<ProjectWriteOffDTO> UpdateWriteOffAsync(UpdateProjectWriteOffDTO model, int employeeId)
        {
            if (model.Amount <= 0)
                throw new Exception("قيمة الخصم يجب أن تكون أكبر من صفر");

            var writeOff = await _unitOfWork.ProjectWriteOff.GetQueryable(w => w.Id == model.Id)
                .Include(w => w.Project).ThenInclude(p => p.Payments)
                .Include(w => w.Project).ThenInclude(p => p.WriteOffs)
                .FirstOrDefaultAsync() ?? throw new Exception("خصم الأعمال غير موجود");

            var otherWriteOffsTotal = writeOff.Project.WriteOffs.Where(w => w.Id != writeOff.Id).Sum(w => w.Amount);
            var remainingExcludingThis = writeOff.Project.ContractValue - writeOff.Project.TotalCollected - otherWriteOffsTotal;
            if (model.Amount > remainingExcludingThis)
                throw new Exception($"قيمة الخصم أكبر من المتبقي على المشروع (المتبقي: {remainingExcludingThis})");

            writeOff.Amount = model.Amount;
            writeOff.Reason = model.Reason;
            writeOff.WriteOffDate = model.WriteOffDate;
            _unitOfWork.ProjectWriteOff.Update(writeOff);

            await _unitOfWork.ProjectAudit.AddAsync(new ProjectAudit
            {
                ProjectId = writeOff.ProjectId,
                EmployeeId = employeeId,
                ActionDate = TimeHelper.NowInEgypt,
                ActionDescription = $"تعديل خصم أعمال ليصبح بقيمة {model.Amount}"
            });

            await _unitOfWork.CompleteAsync();

            return new ProjectWriteOffDTO { Id = writeOff.Id, Amount = writeOff.Amount, Reason = writeOff.Reason, WriteOffDate = writeOff.WriteOffDate };
        }

        public async Task<bool> DeleteWriteOffAsync(int id, int employeeId)
        {
            var writeOff = await _unitOfWork.ProjectWriteOff.GetByIdAsync(id);
            if (writeOff == null) return false;

            _unitOfWork.ProjectWriteOff.Delete(writeOff);

            await _unitOfWork.ProjectAudit.AddAsync(new ProjectAudit
            {
                ProjectId = writeOff.ProjectId,
                EmployeeId = employeeId,
                ActionDate = TimeHelper.NowInEgypt,
                ActionDescription = $"حذف خصم أعمال كان بقيمة {writeOff.Amount}"
            });

            await _unitOfWork.CompleteAsync();
            return true;
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
            Name = p.Name,
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
