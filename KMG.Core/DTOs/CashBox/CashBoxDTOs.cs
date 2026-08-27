namespace KMG.Core.DTOs.CashBox
{
    public class CashBoxDetailsDTO
    {
        public int Id { get; set; }
        public decimal TotalCash { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal TotalBalance { get; set; }
        public List<CashBoxTransactionDTO> RecentTransactions { get; set; } = new();
    }

    public class CashBoxTransactionDTO
    {
        public int Id { get; set; }
        public decimal AmountCash { get; set; }
        public decimal AmountCredit { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public string? ProjectCode { get; set; }
        public string? SupplierName { get; set; }
        public string CreatedByEmployeeName { get; set; } = string.Empty;
    }
}
