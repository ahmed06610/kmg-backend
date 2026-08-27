namespace KMG.Core.Models
{
    public class ProjectAudit
    {
        public int Id { get; set; }
        public string ActionDescription { get; set; } = string.Empty;
        public DateTime ActionDate { get; set; }

        public int ProjectId { get; set; }
        public virtual Project Project { get; set; } = null!;

        public int EmployeeId { get; set; }
        public virtual Employee Employee { get; set; } = null!;
    }
}
