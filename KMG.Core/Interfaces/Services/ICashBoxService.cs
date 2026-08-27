using KMG.Core.DTOs.CashBox;
using KMG.Core.Enums;

namespace KMG.Core.Interfaces.Services
{
    public interface ICashBoxService
    {
        Task<CashBoxDetailsDTO> GetDetailsAsync(int recentCount = 50);

        /// <summary>
        /// يسجل حركة في الخزنة المركزية ويحدّث رصيدها.
        /// مرر amountCash/amountCredit موجب لو الفلوس داخلة للخزنة، وسالب لو خارجة منها.
        /// </summary>
        Task RecordTransactionAsync(
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
            int? advanceId = null);
    }
}
