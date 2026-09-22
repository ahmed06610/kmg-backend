using System.Linq.Expressions;
using KMG.Core.DTOs.CashBox;
using KMG.Core.Enums;
using KMG.Core.Models;

namespace KMG.Core.Interfaces.Services
{
    public interface ICashBoxService
    {
        Task<CashBoxDetailsDTO> GetDetailsAsync(int recentCount = 50);

        Task<PagedResultDTO<CashBoxTransactionDTO>> GetTransactionsAsync(CashBoxTransactionFilterDTO filter);

        Task<MiscExpenseDTO> CreateMiscExpenseAsync(CreateMiscExpenseDTO model, int createdByEmployeeId);
        Task<MiscExpenseDTO> UpdateMiscExpenseAsync(UpdateMiscExpenseDTO model, int employeeId);
        Task<bool> DeleteMiscExpenseAsync(int id);

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
            int? advanceId = null,
            int? projectPaymentId = null,
            int? supplierPaymentId = null,
            int? miscExpenseId = null);

        /// <summary>
        /// يلغي أثر كل حركات الخزنة اللي بتطابق الفلتر (بيطرح مبالغها من رصيد الخزنة) ويمسحها،
        /// عشان يبقى ممكن حذف/تعديل السجل المصدر (دفعة/مصروف/مأمورية/سلفة) من غير ما يفضل رصيد غلط
        /// أو يترفض الحذف بسبب الـ FK Restrict على CashBoxTransaction.
        /// </summary>
        Task ReverseAsync(Expression<Func<CashBoxTransaction, bool>> filter);
    }
}
