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
            int? payrollPayoutId = null)
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
                CreatedByEmployeeId = createdByEmployeeId
            };

            await _unitOfWork.CashBoxTransaction.AddAsync(transaction);
        }

        public async Task<CashBoxDetailsDTO> GetDetailsAsync(int recentCount = 50)
        {
            var cashBox = await GetOrCreateCashBoxAsync();
            await _unitOfWork.CompleteAsync();

            var transactions = await _unitOfWork.CashBoxTransaction
                .GetQueryable(t => t.CashBoxId == cashBox.Id)
                .Include(t => t.Project)
                .Include(t => t.Supplier)
                .Include(t => t.CreatedByEmployee)
                .OrderByDescending(t => t.TransactionDate)
                .Take(recentCount)
                .ToListAsync();

            return new CashBoxDetailsDTO
            {
                Id = cashBox.Id,
                TotalCash = cashBox.TotalCash,
                TotalCredit = cashBox.TotalCredit,
                TotalBalance = cashBox.TotalBalance,
                RecentTransactions = transactions.Select(t => new CashBoxTransactionDTO
                {
                    Id = t.Id,
                    AmountCash = t.AmountCash,
                    AmountCredit = t.AmountCredit,
                    TransactionType = t.TransactionType.ToString(),
                    Description = t.Description,
                    TransactionDate = t.TransactionDate,
                    ProjectCode = t.Project?.ProjectCode,
                    SupplierName = t.Supplier?.Name,
                    CreatedByEmployeeName = t.CreatedByEmployee.Name
                }).ToList()
            };
        }
    }
}
