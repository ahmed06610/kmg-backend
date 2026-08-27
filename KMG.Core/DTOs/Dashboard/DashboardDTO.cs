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
    }

    public class ProjectsByTypeDTO
    {
        public string ProjectType { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalValue { get; set; }
    }
}
