namespace KMG.Core.Models
{
    public class ProjectAttachment
    {
        public int Id { get; set; }
        public string FileUrl { get; set; } = string.Empty;
        public string? FileName { get; set; }
        public string? Description { get; set; }
        public DateTime UploadedAt { get; set; }

        public int ProjectId { get; set; }
        public virtual Project Project { get; set; } = null!;

        public int UploadedByEmployeeId { get; set; }
        public virtual Employee UploadedByEmployee { get; set; } = null!;
    }
}
