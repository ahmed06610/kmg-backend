using KMG.Core.Enums;

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
        public string? ProjectName { get; set; }
        public string? SupplierName { get; set; }
        public string CreatedByEmployeeName { get; set; } = string.Empty;
        public int? MiscExpenseId { get; set; }
        public string? MiscExpenseNotes { get; set; }
        public string? MiscExpenseCategory { get; set; }
        public DateTime? MiscExpenseDate { get; set; }
    }

    public class PagedResultDTO<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class CashBoxTransactionFilterDTO
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public TransactionType? Type { get; set; }
        /// <summary>true = داخل للخزنة (مجموع Cash+Credit موجب)، false = خارج، null = الكل</summary>
        public bool? IsIn { get; set; }
        public string? Search { get; set; }
    }

    public class MiscExpenseDTO
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string? Notes { get; set; }
        public string Category { get; set; } = string.Empty;
        public DateTime ExpenseDate { get; set; }
        public string CreatedByEmployeeName { get; set; } = string.Empty;
    }

    public class CreateMiscExpenseDTO
    {
        public decimal Amount { get; set; }
        public string? Notes { get; set; }
        public MiscExpenseCategory Category { get; set; }
        public DateTime ExpenseDate { get; set; }
    }

    public class UpdateMiscExpenseDTO
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string? Notes { get; set; }
        public MiscExpenseCategory Category { get; set; }
        public DateTime ExpenseDate { get; set; }
    }
}
