using KMG.Core.Enums;

namespace KMG.Core.Models
{
    public class Employee
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public EmployeeType EmployeeType { get; set; }
        public WageType WageType { get; set; }
        public decimal WageAmount { get; set; } // راتب شهري ثابت أو يومية العامل
        public bool Suspended { get; set; }
        public DateTime CreatedAt { get; set; }

        public int? ManagerId { get; set; }
        public virtual Employee? Manager { get; set; }

        // Nullable عمدًا: العمال (المأموريات) عندهم سجل موظف بس من غير حساب دخول للنظام.
        // الإداريين/المحاسبين بس هم اللي محتاجين ApplicationUser فعلي عشان يسجلوا دخول.
        public string? ApplicationUserId { get; set; }
        public virtual ApplicationUser? ApplicationUser { get; set; }

        public virtual ICollection<Advance> Advances { get; set; } = new List<Advance>();
        public virtual ICollection<PayrollAdjustment> PayrollAdjustments { get; set; } = new List<PayrollAdjustment>();
        public virtual ICollection<PayrollPayout> PayrollPayouts { get; set; } = new List<PayrollPayout>();
        public virtual ICollection<MissionWorker> MissionsAsWorker { get; set; } = new List<MissionWorker>();
        public virtual ICollection<Mission> MissionsAsForeman { get; set; } = new List<Mission>();
    }
}
