using KMG.Core.DTOs.Dashboard;
using KMG.Core.Enums;
using KMG.Core.Helper;
using KMG.Core.Interfaces;
using KMG.Core.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace KMG.Core.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IUnitOfWork _unitOfWork;

        public DashboardService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<DashboardDTO> GetDashboardAsync()
        {
            var projects = await _unitOfWork.Project.GetQueryable(null)
                .Include(p => p.Payments)
                .Include(p => p.Expenses)
                .Include(p => p.StockMovements)
                .Include(p => p.Missions).ThenInclude(m => m.MissionWorkers).ThenInclude(w => w.Employee)
                .ToListAsync();

            var totalIncome = projects.Sum(p => p.TotalCollected);
            var totalExpenses = projects.Sum(p => p.TotalMaterialsCost + p.TotalPettyExpenses + p.TotalLaborCost);

            var materials = await _unitOfWork.Material.GetAllAsync();
            var lowStock = materials.Where(m => m.IsLowStock).ToList();

            var cashBoxes = await _unitOfWork.CashBox.GetAllAsync();
            var cashBox = cashBoxes.FirstOrDefault();

            var suppliers = await _unitOfWork.Supplier.GetQueryable(null)
                .Include(s => s.StockMovements)
                .Include(s => s.Payments)
                .ToListAsync();

            var suppliersWithBalance = suppliers.Count(s =>
                s.StockMovements.Where(m => m.MovementType == MovementType.Purchase).Sum(m => m.Quantity * m.UnitPriceAtTime)
                - s.Payments.Sum(p => p.Amount) > 0);

            var now = TimeHelper.NowInEgypt;
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var supplierPaymentsThisMonth = suppliers.SelectMany(s => s.Payments)
                .Where(p => p.PaymentDate >= monthStart)
                .Sum(p => p.Amount);

            return new DashboardDTO
            {
                TotalIncome = totalIncome,
                TotalExpenses = totalExpenses,
                NetProfit = totalIncome - totalExpenses,
                ActiveProjectsCount = projects.Count(p => p.Status != ProjectStatus.Closed && p.Status != ProjectStatus.Completed),
                CompletedProjectsCount = projects.Count(p => p.Status == ProjectStatus.Closed || p.Status == ProjectStatus.Completed),
                ProjectsByType = projects.GroupBy(p => p.ProjectType).Select(g => new ProjectsByTypeDTO
                {
                    ProjectType = g.Key.ToString(),
                    Count = g.Count(),
                    TotalValue = g.Sum(p => p.ContractValue)
                }).ToList(),
                LowStockMaterialsCount = lowStock.Count,
                LowStockMaterialNames = lowStock.Select(m => m.Name).ToList(),
                CashBoxCash = cashBox?.TotalCash ?? 0,
                CashBoxCredit = cashBox?.TotalCredit ?? 0,
                CashBoxTotal = cashBox?.TotalBalance ?? 0,
                SuppliersWithOutstandingBalanceCount = suppliersWithBalance,
                SupplierPaymentsThisMonth = supplierPaymentsThisMonth
            };
        }
    }
}
