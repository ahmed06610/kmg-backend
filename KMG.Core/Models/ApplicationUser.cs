using Microsoft.AspNetCore.Identity;

namespace KMG.Core.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
