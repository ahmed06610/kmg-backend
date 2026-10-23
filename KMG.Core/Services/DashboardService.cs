using KMG.Core.DTOs.Dashboard;
using KMG.Core.Enums;
using KMG.Core.Helper;
using KMG.Core.Interfaces;
using KMG.Core.Interfaces.Services;
using KMG.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace KMG.Core.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IUnitOfWork _unitOfWork;

        private static readonly string[] ArabicMonths =
        {
            "يناير", "فبراير", "مارس", "إبريل", "مايو", "يونيو",
            "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر"
        };

        public DashboardService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<DashboardDTO> GetDashboardAsync(DateTime? from = null, DateTime? to = null)
        {
            // AsSplitQuery() لتفادي انفجار عدد الصفوف من ضرب الكولكشنز في بعض (نفس مشكلة ProjectService)
            var projects = await _unitOfWork.Project.GetQueryable(null)
                .Include(p => p.Client)
                .Include(p => p.Payments)
                .Include(p => p.Expenses)
                .Include(p => p.StockMovements)
                .Include(p => p.Custodies)
                .Include(p => p.Missions).ThenInclude(m => m.MissionWorkers).ThenInclude(w => w.Employee)
                .AsSplitQuery()
                .ToListAsync();

            var hasFilter = from.HasValue || to.HasValue;
            var filterFrom = from ?? DateTime.MinValue;
            var filterTo = to.HasValue ? to.Value.Date.AddDays(1).AddTicks(-1) : DateTime.MaxValue;

            decimal totalContractValue, totalIncome, totalExpenses;
            if (hasFilter)
            {
                totalContractValue = projects.Where(p => p.CreatedAt >= filterFrom && p.CreatedAt <= filterTo).Sum(p => p.ContractValue);
                totalIncome = projects.SelectMany(p => p.Payments).Where(p => p.CheckStatus != CheckStatus.Cancelled && p.PaymentDate >= filterFrom && p.PaymentDate <= filterTo).Sum(p => p.Amount);

                var pettyExpensesInRange = projects.SelectMany(p => p.Expenses)
                    .Where(e => e.ExpenseDate >= filterFrom && e.ExpenseDate <= filterTo).Sum(e => e.Amount);
                var materialsCostInRange = projects.SelectMany(p => p.StockMovements)
                    .Where(m => m.MovementType == MovementType.IssueToProject && m.MovementDate >= filterFrom && m.MovementDate <= filterTo)
                    .Sum(m => m.Quantity * m.UnitPriceAtTime);
                var laborCostInRange = projects.SelectMany(p => p.Missions)
                    .Where(m => m.StartDate >= filterFrom && m.StartDate <= filterTo).Sum(m => m.TotalLaborCost);
                var custodyCostInRange = projects.SelectMany(p => p.Missions)
                    .Where(m => m.StartDate >= filterFrom && m.StartDate <= filterTo)
                    .Sum(m => m.Status == MissionStatus.Settled ? Math.Min(m.ActualSpent, m.AdvanceAmount) : m.AdvanceAmount)
                    + projects.SelectMany(p => p.Custodies)
                    .Where(c => c.IssueDate >= filterFrom && c.IssueDate <= filterTo)
                    .Sum(c => c.Status == CustodyStatus.Settled ? c.SettledAmount ?? c.Amount : c.Amount);
                totalExpenses = pettyExpensesInRange + materialsCostInRange + laborCostInRange + custodyCostInRange;
            }
            else
            {
                totalContractValue = projects.Sum(p => p.ContractValue);
                totalIncome = projects.Sum(p => p.TotalCollected);
                totalExpenses = projects.Sum(p => p.TotalCost);
            }

            // رصيد مستحق للتحصيل ورصيد الخزنة أرقام لحظية (مش مرتبطة بفترة)، فبتفضل زي ما هي بغض النظر عن الفلتر
            var totalReceivables = projects.Sum(p => p.RemainingBalance);

            var materials = await _unitOfWork.Material.GetAllAsync();
            var lowStock = materials.Where(m => m.IsLowStock).ToList();

            var cashBoxes = await _unitOfWork.CashBox.GetAllAsync();
            var cashBox = cashBoxes.FirstOrDefault();

            var suppliers = await _unitOfWork.Supplier.GetQueryable(null)
                .Include(s => s.StockMovements)
                .Include(s => s.Payments)
                .AsSplitQuery()
                .ToListAsync();

            var supplierBalances = suppliers.Select(s => new
            {
                s.Name,
                Remaining = s.StockMovements.Where(m => m.MovementType == MovementType.Purchase).Sum(m => m.Quantity * m.UnitPriceAtTime + m.TransportCost)
                    - s.Payments.Where(p => p.CheckStatus != CheckStatus.Cancelled).Sum(p => p.Amount)
            }).ToList();

            var suppliersWithBalance = supplierBalances.Count(s => s.Remaining > 0);
            var totalPayables = supplierBalances.Where(s => s.Remaining > 0).Sum(s => s.Remaining);
            var topSupplier = supplierBalances.Where(s => s.Remaining > 0).OrderByDescending(s => s.Remaining).FirstOrDefault();

            var now = TimeHelper.NowInEgypt;
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var supplierPaymentsThisMonth = suppliers.SelectMany(s => s.Payments)
                .Where(p => p.PaymentDate >= monthStart)
                .Sum(p => p.Amount);

            var openMissionsCount = projects.SelectMany(p => p.Missions).Count(m => m.Status == MissionStatus.Open);

            var totalOutstandingAdvances = (await _unitOfWork.Advance.GetAllAsync())
                .Where(a => a.Status == AdvanceStatus.Active)
                .Sum(a => a.RemainingAmount);

            var clientBalances = projects.GroupBy(p => p.ClientId)
                .Select(g => new { Name = g.First().Client.Name, Remaining = g.Sum(p => p.RemainingBalance) })
                .ToList();
            var clientsWithBalance = clientBalances.Count(c => c.Remaining > 0);
            var topClient = clientBalances.Where(c => c.Remaining > 0).OrderByDescending(c => c.Remaining).FirstOrDefault();

            var recentActivity = (await _unitOfWork.CashBoxTransaction.GetAllAsync())
                .OrderByDescending(t => t.TransactionDate)
                .Take(6)
                .Select(t => new RecentActivityDTO
                {
                    Description = t.Description,
                    Amount = t.AmountCash + t.AmountCredit,
                    Date = t.TransactionDate
                }).ToList();

            var pipeline = new ProjectsPipelineDTO
            {
                NewCount = projects.Count(p => p.Status == ProjectStatus.New),
                InProgressCount = projects.Count(p => p.Status is ProjectStatus.MaterialsReview or ProjectStatus.PurchaseOrder or ProjectStatus.Manufacturing or ProjectStatus.Mission),
                CompletedCount = projects.Count(p => p.Status is ProjectStatus.Completed or ProjectStatus.Closed)
            };

            var activeProjectsSummary = projects
                .Where(p => p.Status != ProjectStatus.Closed && p.Status != ProjectStatus.Completed)
                .OrderByDescending(p => p.CreatedAt)
                .Take(4)
                .Select(p => new ActiveProjectSummaryDTO
                {
                    Id = p.Id,
                    Name = p.Name,
                    ProjectCode = p.ProjectCode,
                    ClientName = p.Client.Name,
                    Status = p.Status.ToString(),
                    ContractValue = p.ContractValue,
                    TotalCollected = p.TotalCollected,
                    ActualCost = p.TotalCost,
                    NetProfit = p.NetProfit,
                    ProgressPercent = ProjectProgressPercent(p.Status)
                }).ToList();

            var trendMonths = hasFilter
                ? MonthsInRange(from ?? projects.Min(p => (DateTime?)p.CreatedAt) ?? now, to ?? now)
                : Enumerable.Range(0, 6).Select(i => new DateTime(now.Year, now.Month, 1).AddMonths(-5 + i)).ToList();
            var monthlyTrend = BuildMonthlyTrend(projects, trendMonths);
            var (incomeChange, expensesChange, netProfitChange, contractValueChange) = ComputeMonthOverMonthChanges(monthlyTrend, projects, now);

            return new DashboardDTO
            {
                TotalContractValue = totalContractValue,
                TotalContractValueChangePercent = contractValueChange,
                TotalIncome = totalIncome,
                IncomeChangePercent = incomeChange,
                TotalExpenses = totalExpenses,
                ExpensesChangePercent = expensesChange,
                NetProfit = totalIncome - totalExpenses,
                NetProfitChangePercent = netProfitChange,
                CashBoxCash = cashBox?.TotalCash ?? 0,
                CashBoxCredit = cashBox?.TotalCredit ?? 0,
                CashBoxTotal = cashBox?.TotalBalance ?? 0,
                TotalReceivables = totalReceivables,
                TotalPayables = totalPayables,
                ActiveProjectsCount = projects.Count(p => p.Status != ProjectStatus.Closed && p.Status != ProjectStatus.Completed),
                CompletedProjectsCount = projects.Count(p => p.Status == ProjectStatus.Closed || p.Status == ProjectStatus.Completed),
                ProjectsByType = projects.GroupBy(p => p.ProjectType).Select(g => new ProjectsByTypeDTO
                {
                    ProjectType = g.Key.ToString(),
                    Count = g.Count(),
                    TotalValue = g.Sum(p => p.ContractValue)
                }).ToList(),
                ProjectsPipeline = pipeline,
                ActiveProjectsSummary = activeProjectsSummary,
                MonthlyTrend = monthlyTrend,
                LowStockMaterialsCount = lowStock.Count,
                LowStockMaterials = lowStock.Select(m => new LowStockMaterialDTO
                {
                    Name = m.Name,
                    Quantity = m.Quantity,
                    Unit = m.Unit,
                    MinimumThreshold = m.MinimumThreshold
                }).ToList(),
                SuppliersWithOutstandingBalanceCount = suppliersWithBalance,
                SupplierPaymentsThisMonth = supplierPaymentsThisMonth,
                TopOutstandingSupplier = topSupplier == null ? null : new TopOutstandingDTO { Name = topSupplier.Name, Amount = topSupplier.Remaining },
                ClientsWithOutstandingBalanceCount = clientsWithBalance,
                TopOutstandingClient = topClient == null ? null : new TopOutstandingDTO { Name = topClient.Name, Amount = topClient.Remaining },
                OpenMissionsCount = openMissionsCount,
                TotalOutstandingAdvances = totalOutstandingAdvances,
                RecentActivity = recentActivity
            };
        }

        // مشروع مغلق أو مكتمل = 100%؛ باقي المراحل بتتوزع بالتساوي بين "جديد" (0%) و"مأمورية" (80%)
        private static int ProjectProgressPercent(ProjectStatus status) => status switch
        {
            ProjectStatus.New => 0,
            ProjectStatus.MaterialsReview => 20,
            ProjectStatus.PurchaseOrder => 40,
            ProjectStatus.Manufacturing => 60,
            ProjectStatus.Mission => 80,
            ProjectStatus.Completed => 100,
            ProjectStatus.Closed => 100,
            _ => 0
        };

        // أقصى عدد شهور نبنيهم في الرسم البياني لو الفلتر غطى مدى طويل جدًا (حماية من استجابة ضخمة)
        private const int MaxTrendMonths = 36;

        private static List<DateTime> MonthsInRange(DateTime from, DateTime to)
        {
            var start = new DateTime(from.Year, from.Month, 1);
            var end = new DateTime(to.Year, to.Month, 1);
            var months = new List<DateTime>();
            var cursor = start;
            while (cursor <= end && months.Count < MaxTrendMonths)
            {
                months.Add(cursor);
                cursor = cursor.AddMonths(1);
            }
            return months;
        }

        private List<MonthlyTrendDTO> BuildMonthlyTrend(List<Project> projects, List<DateTime> months)
        {
            var payments = projects.SelectMany(p => p.Payments).ToList();
            var expenses = projects.SelectMany(p => p.Expenses).ToList();
            var issuedMaterials = projects.SelectMany(p => p.StockMovements)
                .Where(m => m.MovementType == MovementType.IssueToProject)
                .ToList();

            var spansMultipleYears = months.Select(m => m.Year).Distinct().Count() > 1;

            return months.Select(monthStart =>
            {
                var monthEnd = monthStart.AddMonths(1);
                var income = payments.Where(p => p.PaymentDate >= monthStart && p.PaymentDate < monthEnd).Sum(p => p.Amount);
                var pettyExpenses = expenses.Where(e => e.ExpenseDate >= monthStart && e.ExpenseDate < monthEnd).Sum(e => e.Amount);
                var materialsCost = issuedMaterials.Where(m => m.MovementDate >= monthStart && m.MovementDate < monthEnd).Sum(m => m.Quantity * m.UnitPriceAtTime);
                var expensesTotal = pettyExpenses + materialsCost;
                var label = ArabicMonths[monthStart.Month - 1] + (spansMultipleYears ? $" {monthStart.Year}" : "");
                return new MonthlyTrendDTO
                {
                    MonthLabel = label,
                    Income = income,
                    Expenses = expensesTotal,
                    NetProfit = income - expensesTotal
                };
            }).ToList();
        }

        private static (decimal income, decimal expenses, decimal netProfit, decimal contractValue) ComputeMonthOverMonthChanges(
            List<MonthlyTrendDTO> monthlyTrend, List<Project> projects, DateTime now)
        {
            decimal PercentChange(decimal current, decimal previous) => previous == 0 ? 0 : Math.Round((current - previous) / previous * 100, 1);

            var current = monthlyTrend.LastOrDefault();
            var previous = monthlyTrend.Count >= 2 ? monthlyTrend[^2] : null;
            if (current == null || previous == null)
                return (0, 0, 0, 0);

            var thisMonthStart = new DateTime(now.Year, now.Month, 1);
            var lastMonthStart = thisMonthStart.AddMonths(-1);
            var contractValueThisMonth = projects.Where(p => p.CreatedAt >= thisMonthStart).Sum(p => p.ContractValue);
            var contractValueLastMonth = projects.Where(p => p.CreatedAt >= lastMonthStart && p.CreatedAt < thisMonthStart).Sum(p => p.ContractValue);

            return (
                PercentChange(current.Income, previous.Income),
                PercentChange(current.Expenses, previous.Expenses),
                PercentChange(current.NetProfit, previous.NetProfit),
                PercentChange(contractValueThisMonth, contractValueLastMonth)
            );
        }
    }
}
