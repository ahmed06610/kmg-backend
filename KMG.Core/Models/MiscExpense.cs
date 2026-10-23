using System.ComponentModel.DataAnnotations.Schema;
using KMG.Core.Enums;

namespace KMG.Core.Models
{
    /// <summary>
    /// مصروف نثري عام مش مرتبط بمشروع معين (إداري/تشغيلي/أخرى) - بيتسجل من صفحة الخزنة مباشرة
    /// ويخصم من رصيدها فورًا، على عكس ProjectExpense اللي لازم يكون مربوط بمشروع.
    /// </summary>
    public class MiscExpense
    {
        public int Id { get; set; }
        public decimal AmountCash { get; set; }
        public decimal AmountCredit { get; set; }

        [NotMapped]
        public decimal Amount => AmountCash + AmountCredit;

        public string? Notes { get; set; }
        public MiscExpenseCategory Category { get; set; }
        public DateTime ExpenseDate { get; set; }
        public string? AttachmentUrl { get; set; }
        public string? AttachmentFileName { get; set; }

        public int CreatedByEmployeeId { get; set; }
        public virtual Employee CreatedByEmployee { get; set; } = null!;
    }
}
