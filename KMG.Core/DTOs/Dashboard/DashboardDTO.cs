namespace KMG.Core.DTOs.Dashboard
{
    public class DashboardDTO
    {
        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetProfit { get; set; }

        public int ActiveProjectsCount { get; set; }
        public int CompletedProjectsCount { get; set; }
        public List<ProjectsByTypeDTO> ProjectsByType { get; set; } = new();

        public int LowStockMaterialsCount { get; set; }
        public List<string> LowStockMaterialNames { get; set; } = new();

        public decimal CashBoxCash { get; set; }
        public decimal CashBoxCredit { get; set; }
        public decimal CashBoxTotal { get; set; }

        public int SuppliersWithOutstandingBalanceCount { get; set; }
        public decimal SupplierPaymentsThisMonth { get; set; }

        public int OpenMissionsCount { get; set; }
        public decimal TotalOutstandingAdvances { get; set; }
        public int ClientsWithOutstandingBalanceCount { get; set; }
        public List<RecentActivityDTO> RecentActivity { get; set; } = new();
    }

    public class ProjectsByTypeDTO
    {
        public string ProjectType { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalValue { get; set; }
    }

    public class RecentActivityDTO
    {
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
    }
}
