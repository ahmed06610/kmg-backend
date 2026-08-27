namespace KMG.Core.Models
{
    public class MissionWorker
    {
        public int Id { get; set; }
        public int DaysCount { get; set; }

        public int MissionId { get; set; }
        public virtual Mission Mission { get; set; } = null!;

        public int EmployeeId { get; set; }
        public virtual Employee Employee { get; set; } = null!;
    }
}
