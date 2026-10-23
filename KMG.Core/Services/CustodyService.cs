using KMG.Core.DTOs.CashBox;
using KMG.Core.Enums;
using KMG.Core.Helper;
using KMG.Core.Interfaces;
using KMG.Core.Interfaces.Services;
using KMG.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace KMG.Core.Services
{
    public class CustodyService : ICustodyService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICashBoxService _cashBoxService;

        public CustodyService(IUnitOfWork unitOfWork, ICashBoxService cashBoxService)
        {
            _unitOfWork = unitOfWork;
            _cashBoxService = cashBoxService;
        }

        public async Task<List<CustodyDTO>> GetAllAsync()
        {
            var custodies = await _unitOfWork.Custody.GetQueryable(null)
                .Include(c => c.Employee)
                .Include(c => c.Project)
                .OrderByDescending(c => c.IssueDate)
                .ToListAsync();

            return custodies.Select(MapCustody).ToList();
        }

        public async Task<CustodyDTO> CreateAsync(CreateCustodyDTO model, int createdByEmployeeId)
        {
            if (model.AmountCash + model.AmountCredit <= 0)
                throw new Exception("قيمة العهدة يجب أن تكون أكبر من صفر");

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var employee = await _unitOfWork.Employee.GetByIdAsync(model.EmployeeId)
                    ?? throw new Exception("الموظف غير موجود");

                var custody = new Custody
                {
                    EmployeeId = model.EmployeeId,
                    AmountCash = model.AmountCash,
                    AmountCredit = model.AmountCredit,
                    Description = model.Description,
                    IssueDate = model.IssueDate,
                    Status = CustodyStatus.Active,
                    ProjectId = model.ProjectId,
                    Notes = model.Notes
                };

                await _unitOfWork.Custody.AddAsync(custody);
                await _unitOfWork.CompleteAsync(); // نحتاج custody.Id عشان نربط بيه حركة الخزنة

                await _cashBoxService.RecordTransactionAsync(
                    amountCash: -model.AmountCash,
                    amountCredit: -model.AmountCredit,
                    type: TransactionType.CustodyOut,
                    description: $"صرف عهدة جانبية للموظف: {employee.Name} - {model.Description}",
                    createdByEmployeeId: createdByEmployeeId,
                    projectId: model.ProjectId,
                    custodyId: custody.Id);

                await AddProjectAuditAsync(model.ProjectId, createdByEmployeeId, $"صرف عهدة للموظف {employee.Name} بقيمة {custody.Amount}: {model.Description}");

                await _unitOfWork.CompleteAsync();
                await transaction.CommitAsync();

                return (await GetByIdAsync(custody.Id))!;
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<CustodyDTO> UpdateAsync(UpdateCustodyDTO model, int employeeId)
        {
            if (model.AmountCash + model.AmountCredit <= 0)
                throw new Exception("قيمة العهدة يجب أن تكون أكبر من صفر");

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var custody = await _unitOfWork.Custody.GetQueryable(c => c.Id == model.Id)
                    .Include(c => c.Employee)
                    .FirstOrDefaultAsync() ?? throw new Exception("العهدة غير موجودة");

                var employee = model.EmployeeId == custody.EmployeeId
                    ? custody.Employee
                    : await _unitOfWork.Employee.GetByIdAsync(model.EmployeeId) ?? throw new Exception("الموظف غير موجود");

                await _cashBoxService.ReverseAsync(t => t.CustodyId == custody.Id);

                custody.EmployeeId = model.EmployeeId;
                custody.AmountCash = model.AmountCash;
                custody.AmountCredit = model.AmountCredit;
                custody.Description = model.Description;
                custody.IssueDate = model.IssueDate;
                custody.ProjectId = model.ProjectId;
                custody.Notes = model.Notes;
                _unitOfWork.Custody.Update(custody);

                await _cashBoxService.RecordTransactionAsync(
                    amountCash: -model.AmountCash,
                    amountCredit: -model.AmountCredit,
                    type: TransactionType.CustodyOut,
                    description: $"صرف عهدة جانبية للموظف: {employee.Name} - {model.Description}",
                    createdByEmployeeId: employeeId,
                    projectId: model.ProjectId,
                    custodyId: custody.Id);

                if (custody.Status == CustodyStatus.Settled && custody.SettledAmount.HasValue)
                    await RecordSettlementDifferenceAsync(custody, employeeId);

                await _unitOfWork.CompleteAsync();
                await transaction.CommitAsync();

                return (await GetByIdAsync(custody.Id))!;
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id, int employeeId)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var custody = await _unitOfWork.Custody.GetByIdAsync(id);
                if (custody == null) return false;

                await _cashBoxService.ReverseAsync(t => t.CustodyId == id);
                _unitOfWork.Custody.Delete(custody);
                await AddProjectAuditAsync(custody.ProjectId, employeeId, $"حذف عهدة بقيمة {custody.Amount}: {custody.Description}");

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

        public async Task<CustodyDTO> SettleAsync(SettleCustodyDTO model, int settledByEmployeeId)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var custody = await _unitOfWork.Custody.GetQueryable(c => c.Id == model.CustodyId)
                    .Include(c => c.Employee)
                    .FirstOrDefaultAsync() ?? throw new Exception("العهدة غير موجودة");

                if (custody.Status == CustodyStatus.Settled)
                    throw new Exception("العهدة دي متسواة بالفعل");

                custody.SettledAmount = model.SettledAmount;
                custody.SettledDate = model.SettledDate;
                custody.Status = CustodyStatus.Settled;
                if (!string.IsNullOrWhiteSpace(model.Notes)) custody.Notes = model.Notes;
                _unitOfWork.Custody.Update(custody);

                await RecordSettlementDifferenceAsync(custody, settledByEmployeeId);
                await AddProjectAuditAsync(custody.ProjectId, settledByEmployeeId,
                    $"تسوية عهدة {custody.Employee.Name}: المصروف الفعلي {model.SettledAmount} من أصل {custody.Amount}");

                await _unitOfWork.CompleteAsync();
                await transaction.CommitAsync();

                return (await GetByIdAsync(custody.Id))!;
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        private async Task RecordSettlementDifferenceAsync(Custody custody, int employeeId)
        {
            var difference = custody.Amount - custody.SettledAmount!.Value;

            if (difference > 0)
            {
                // فلوس فاضلة من العهدة رجعت للخزنة
                await _cashBoxService.RecordTransactionAsync(
                    amountCash: difference,
                    amountCredit: 0,
                    type: TransactionType.CustodySettlementIn,
                    description: $"استرجاع فارق عهدة جانبية - {custody.Employee.Name}",
                    createdByEmployeeId: employeeId,
                    projectId: custody.ProjectId,
                    custodyId: custody.Id);
            }
            else if (difference < 0)
            {
                // العهدة متكفتش، الفرق ده بيتسدد للموظف
                await _cashBoxService.RecordTransactionAsync(
                    amountCash: difference,
                    amountCredit: 0,
                    type: TransactionType.CustodySettlementOut,
                    description: $"سداد فارق عهدة جانبية - {custody.Employee.Name}",
                    createdByEmployeeId: employeeId,
                    projectId: custody.ProjectId,
                    custodyId: custody.Id);
            }
        }

        // العهد المربوطة بمشروع بتظهر في مصاريفه، فأي حركة عليها لازم تتسجل في سجل تدقيق المشروع كمان
        private async Task AddProjectAuditAsync(int? projectId, int employeeId, string description)
        {
            if (projectId == null) return;
            await _unitOfWork.ProjectAudit.AddAsync(new ProjectAudit
            {
                ProjectId = projectId.Value,
                EmployeeId = employeeId,
                ActionDate = TimeHelper.NowInEgypt,
                ActionDescription = description
            });
        }

        private async Task<CustodyDTO?> GetByIdAsync(int id)
        {
            var custody = await _unitOfWork.Custody.GetQueryable(c => c.Id == id)
                .Include(c => c.Employee)
                .Include(c => c.Project)
                .FirstOrDefaultAsync();

            return custody == null ? null : MapCustody(custody);
        }

        public static CustodyDTO MapCustody(Custody c) => new()
        {
            Id = c.Id,
            Amount = c.Amount,
            AmountCash = c.AmountCash,
            AmountCredit = c.AmountCredit,
            Description = c.Description,
            IssueDate = c.IssueDate,
            Status = c.Status.ToString(),
            SettledAmount = c.SettledAmount,
            SettledDate = c.SettledDate,
            Notes = c.Notes,
            EmployeeId = c.EmployeeId,
            EmployeeName = c.Employee.Name,
            ProjectId = c.ProjectId,
            ProjectName = c.Project?.Name
        };
    }
}
