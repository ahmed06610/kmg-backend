namespace KMG.Core.Models
{
    public class Client
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
    }
}
