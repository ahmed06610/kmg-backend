namespace KMG.Core.DTOs.Dashboard
{
    public class DashboardDTO
    {
        public decimal TotalContractValue { get; set; }
        public decimal TotalContractValueChangePercent { get; set; }

        public decimal TotalIncome { get; set; }
        public decimal IncomeChangePercent { get; set; }

        public decimal TotalExpenses { get; set; }
        public decimal ExpensesChangePercent { get; set; }

        public decimal NetProfit { get; set; }
        public decimal NetProfitChangePercent { get; set; }

        public decimal CashBoxCash { get; set; }
        public decimal CashBoxCredit { get; set; }
        public decimal CashBoxTotal { get; set; }

        public decimal TotalReceivables { get; set; }
        public decimal TotalPayables { get; set; }

        public int ActiveProjectsCount { get; set; }
        public int CompletedProjectsCount { get; set; }
        public List<ProjectsByTypeDTO> ProjectsByType { get; set; } = new();
        public ProjectsPipelineDTO ProjectsPipeline { get; set; } = new();
        public List<ActiveProjectSummaryDTO> ActiveProjectsSummary { get; set; } = new();

        public List<MonthlyTrendDTO> MonthlyTrend { get; set; } = new();

        public int LowStockMaterialsCount { get; set; }
        public List<LowStockMaterialDTO> LowStockMaterials { get; set; } = new();

        public int SuppliersWithOutstandingBalanceCount { get; set; }
        public decimal SupplierPaymentsThisMonth { get; set; }
        public TopOutstandingDTO? TopOutstandingSupplier { get; set; }

        public int ClientsWithOutstandingBalanceCount { get; set; }
        public TopOutstandingDTO? TopOutstandingClient { get; set; }

        public int OpenMissionsCount { get; set; }
        public decimal TotalOutstandingAdvances { get; set; }

        public List<RecentActivityDTO> RecentActivity { get; set; } = new();
    }

    public class ProjectsByTypeDTO
    {
        public string ProjectType { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalValue { get; set; }
    }

    public class ProjectsPipelineDTO
    {
        public int NewCount { get; set; }
        public int InProgressCount { get; set; }
        public int CompletedCount { get; set; }
    }

    public class ActiveProjectSummaryDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ProjectCode { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal ContractValue { get; set; }
        public decimal TotalCollected { get; set; }
        public decimal ActualCost { get; set; }
        public decimal NetProfit { get; set; }
        public int ProgressPercent { get; set; }
    }

    public class MonthlyTrendDTO
    {
        public string MonthLabel { get; set; } = string.Empty;
        public decimal Income { get; set; }
        public decimal Expenses { get; set; }
        public decimal NetProfit { get; set; }
    }

    public class LowStockMaterialDTO
    {
        public string Name { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal MinimumThreshold { get; set; }
    }

    public class TopOutstandingDTO
    {
        public string Name { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class RecentActivityDTO
    {
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
    }
}
