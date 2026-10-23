namespace KMG.Core.Models
{
    public class MissionWorker
    {
        public int Id { get; set; }
        public decimal DaysCount { get; set; } // ممكن يبقى كسر (زي 3.5 أو 2.25) - عدد الأيام مش لازم يبقى رقم صحيح

        // إجمالي الأجر المستحق لليوم الواحد في المأمورية دي لهذا العامل (مش بالضرورة ضعف يوميته العادية
        // زي ما كان مفروض قبل كده - ده بقى اختيار المستخدم بالكامل وقت إضافة العامل للمأمورية)
        public decimal DailyRate { get; set; }

        public int MissionId { get; set; }
        public virtual Mission Mission { get; set; } = null!;

        public int EmployeeId { get; set; }
        public virtual Employee Employee { get; set; } = null!;
    }
}
