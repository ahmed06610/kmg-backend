namespace KMG.Core.Models
{
    /// <summary>
    /// خصم أعمال المشروع: مبلغ متعذر تحصيله من العميل (مش هيتحصل خالص)، بيتخصم من قيمة العقد
    /// عشان "المتبقي" يبقى صحيح ومايفضلش شكله إن لسه فيه فلوس مستحقة. تسوية محاسبية بحتة - مفيش أثر خزنة.
    /// </summary>
    public class ProjectWriteOff
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime WriteOffDate { get; set; }

        public int ProjectId { get; set; }
        public virtual Project Project { get; set; } = null!;

        public int CreatedByEmployeeId { get; set; }
        public virtual Employee CreatedByEmployee { get; set; } = null!;
    }
}
