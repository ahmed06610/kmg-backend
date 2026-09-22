using System.Linq.Expressions;
using KMG.Core.DTOs.CashBox;
using KMG.Core.Enums;
using KMG.Core.Helper;
using KMG.Core.Interfaces;
using KMG.Core.Interfaces.Services;
using KMG.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace KMG.Core.Services
{
    public class CashBoxService : ICashBoxService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CashBoxService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        private async Task<CashBox> GetOrCreateCashBoxAsync()
        {
            var cashBox = (await _unitOfWork.CashBox.GetAllAsync()).FirstOrDefault();
            if (cashBox == null)
            {
                cashBox = new CashBox { TotalCash = 0, TotalCredit = 0 };
                await _unitOfWork.CashBox.AddAsync(cashBox);
                await _unitOfWork.CompleteAsync(); // نحتاج cashBox.Id فورًا لأي حركة هتتسجل بعد كده في نفس العملية
            }
            return cashBox;
        }

        public async Task RecordTransactionAsync(
            decimal amountCash,
            decimal amountCredit,
            TransactionType type,
            string description,
            int createdByEmployeeId,
            int? projectId = null,
            int? supplierId = null,
            int? projectExpenseId = null,
            int? missionId = null,
            int? payrollPayoutId = null,
            int? advanceId = null,
            int? projectPaymentId = null,
            int? supplierPaymentId = null,
            int? miscExpenseId = null)
        {
            var cashBox = await GetOrCreateCashBoxAsync();

            cashBox.TotalCash += amountCash;
            cashBox.TotalCredit += amountCredit;
            _unitOfWork.CashBox.Update(cashBox);

            var transaction = new CashBoxTransaction
            {
                CashBoxId = cashBox.Id,
                AmountCash = amountCash,
                AmountCredit = amountCredit,
                TransactionType = type,
                Description = description,
                TransactionDate = TimeHelper.NowInEgypt,
                ProjectId = projectId,
                SupplierId = supplierId,
                ProjectExpenseId = projectExpenseId,
                MissionId = missionId,
                PayrollPayoutId = payrollPayoutId,
                AdvanceId = advanceId,
                ProjectPaymentId = projectPaymentId,
                SupplierPaymentId = supplierPaymentId,
                MiscExpenseId = miscExpenseId,
                CreatedByEmployeeId = createdByEmployeeId
            };

            await _unitOfWork.CashBoxTransaction.AddAsync(transaction);
        }

        public async Task ReverseAsync(Expression<Func<CashBoxTransaction, bool>> filter)
        {
            var transactions = await _unitOfWork.CashBoxTransaction.GetQueryable(filter).ToListAsync();
            if (transactions.Count == 0) return;

            var cashBox = await GetOrCreateCashBoxAsync();
            cashBox.TotalCash -= transactions.Sum(t => t.AmountCash);
            cashBox.TotalCredit -= transactions.Sum(t => t.AmountCredit);
            _unitOfWork.CashBox.Update(cashBox);

            _unitOfWork.CashBoxTransaction.DeleteRange(transactions);
        }

        public async Task<PagedResultDTO<CashBoxTransactionDTO>> GetTransactionsAsync(CashBoxTransactionFilterDTO filter)
        {
            var query = _unitOfWork.CashBoxTransaction.GetQueryable(null)
                .Include(t => t.Project)
                .Include(t => t.Supplier)
                .Include(t => t.CreatedByEmployee)
                .Include(t => t.MiscExpense)
                .AsQueryable();

            if (filter.DateFrom.HasValue)
                query = query.Where(t => t.TransactionDate >= filter.DateFrom.Value.Date);
            if (filter.DateTo.HasValue)
                query = query.Where(t => t.TransactionDate < filter.DateTo.Value.Date.AddDays(1));
            if (filter.Type.HasValue)
                query = query.Where(t => t.TransactionType == filter.Type.Value);
            if (filter.IsIn.HasValue)
                query = filter.IsIn.Value
                    ? query.Where(t => t.AmountCash + t.AmountCredit >= 0)
                    : query.Where(t => t.AmountCash + t.AmountCredit < 0);
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim();
                query = query.Where(t =>
                    t.Description.Contains(term) ||
                    (t.Project != null && (t.Project.Name.Contains(term) || t.Project.ProjectCode.Contains(term))) ||
                    (t.Supplier != null && t.Supplier.Name.Contains(term)) ||
                    t.CreatedByEmployee.Name.Contains(term));
            }

            var totalCount = await query.CountAsync();
            var page = Math.Max(filter.Page, 1);
            var pageSize = Math.Clamp(filter.PageSize, 1, 200);

            var transactions = await query
                .OrderByDescending(t => t.TransactionDate)
                .ThenByDescending(t => t.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResultDTO<CashBoxTransactionDTO>
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                Items = transactions.Select(MapTransaction).ToList()
            };
        }

        public async Task<MiscExpenseDTO> CreateMiscExpenseAsync(CreateMiscExpenseDTO model, int createdByEmployeeId)
        {
            if (model.Amount <= 0)
                throw new Exception("قيمة المصروف يجب أن تكون أكبر من صفر");

            var expense = new MiscExpense
            {
                Amount = model.Amount,
                Notes = model.Notes,
                Category = model.Category,
                ExpenseDate = model.ExpenseDate,
                CreatedByEmployeeId = createdByEmployeeId
            };

            await _unitOfWork.MiscExpense.AddAsync(expense);
            await _unitOfWork.CompleteAsync(); // نحتاج expense.Id عشان نربط بيه حركة الخزنة

            await RecordTransactionAsync(
                amountCash: -model.Amount,
                amountCredit: 0,
                type: TransactionType.MiscExpenseOut,
                description: $"مصروف نثري ({CategoryLabel(model.Category)}): {model.Notes}",
                createdByEmployeeId: createdByEmployeeId,
                miscExpenseId: expense.Id);

            await _unitOfWork.CompleteAsync();

            var employee = await _unitOfWork.Employee.GetQueryable(e => e.Id == createdByEmployeeId).FirstAsync();
            return MapMiscExpense(expense, employee.Name);
        }

        public async Task<MiscExpenseDTO> UpdateMiscExpenseAsync(UpdateMiscExpenseDTO model, int employeeId)
        {
            if (model.Amount <= 0)
                throw new Exception("قيمة المصروف يجب أن تكون أكبر من صفر");

            var expense = await _unitOfWork.MiscExpense.GetQueryable(e => e.Id == model.Id)
                .Include(e => e.CreatedByEmployee)
                .FirstOrDefaultAsync() ?? throw new Exception("المصروف غير موجود");

            await ReverseAsync(t => t.MiscExpenseId == expense.Id);

            expense.Amount = model.Amount;
            expense.Notes = model.Notes;
            expense.Category = model.Category;
            expense.ExpenseDate = model.ExpenseDate;
            _unitOfWork.MiscExpense.Update(expense);

            await RecordTransactionAsync(
                amountCash: -model.Amount,
                amountCredit: 0,
                type: TransactionType.MiscExpenseOut,
                description: $"تعديل مصروف نثري ({CategoryLabel(model.Category)}): {model.Notes}",
                createdByEmployeeId: employeeId,
                miscExpenseId: expense.Id);

            await _unitOfWork.CompleteAsync();

            return MapMiscExpense(expense, expense.CreatedByEmployee.Name);
        }

        public async Task<bool> DeleteMiscExpenseAsync(int id)
        {
            var expense = await _unitOfWork.MiscExpense.GetByIdAsync(id);
            if (expense == null) return false;

            await ReverseAsync(t => t.MiscExpenseId == id);
            _unitOfWork.MiscExpense.Delete(expense);

            await _unitOfWork.CompleteAsync();
            return true;
        }

        private static string CategoryLabel(Enums.MiscExpenseCategory category) => category switch
        {
            Enums.MiscExpenseCategory.Administrative => "إداري",
            Enums.MiscExpenseCategory.Operational => "تشغيلي",
            _ => "أخرى"
        };

        private static MiscExpenseDTO MapMiscExpense(MiscExpense e, string employeeName) => new()
        {
            Id = e.Id,
            Amount = e.Amount,
            Notes = e.Notes,
            Category = e.Category.ToString(),
            ExpenseDate = e.ExpenseDate,
            CreatedByEmployeeName = employeeName
        };

        private static CashBoxTransactionDTO MapTransaction(CashBoxTransaction t) => new()
        {
            Id = t.Id,
            AmountCash = t.AmountCash,
            AmountCredit = t.AmountCredit,
            TransactionType = t.TransactionType.ToString(),
            Description = t.Description,
            TransactionDate = t.TransactionDate,
            ProjectCode = t.Project?.ProjectCode,
            ProjectName = t.Project?.Name,
            SupplierName = t.Supplier?.Name,
            CreatedByEmployeeName = t.CreatedByEmployee.Name,
            MiscExpenseId = t.MiscExpenseId,
            MiscExpenseNotes = t.MiscExpense?.Notes,
            MiscExpenseCategory = t.MiscExpense?.Category.ToString(),
            MiscExpenseDate = t.MiscExpense?.ExpenseDate
        };

        public async Task<CashBoxDetailsDTO> GetDetailsAsync(int recentCount = 50)
        {
            var cashBox = await GetOrCreateCashBoxAsync();
            await _unitOfWork.CompleteAsync();

            var transactions = await _unitOfWork.CashBoxTransaction
                .GetQueryable(t => t.CashBoxId == cashBox.Id)
                .Include(t => t.Project)
                .Include(t => t.Supplier)
                .Include(t => t.CreatedByEmployee)
                .Include(t => t.MiscExpense)
                .OrderByDescending(t => t.TransactionDate)
                .Take(recentCount)
                .ToListAsync();

            return new CashBoxDetailsDTO
            {
                Id = cashBox.Id,
                TotalCash = cashBox.TotalCash,
                TotalCredit = cashBox.TotalCredit,
                TotalBalance = cashBox.TotalBalance,
                RecentTransactions = transactions.Select(MapTransaction).ToList()
            };
        }
    }
}
