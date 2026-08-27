using Microsoft.AspNetCore.Identity;

namespace KMG.Core.Models
{
    public class RoleIdentity : IdentityRole
    {
        public RoleIdentity() : base() { }

        public RoleIdentity(string roleName) : base(roleName) { }

        public virtual ICollection<RolesAbility> RolesAbilities { get; set; } = new List<RolesAbility>();
    }
}
