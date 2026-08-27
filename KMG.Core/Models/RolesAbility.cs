namespace KMG.Core.Models
{
    public class RolesAbility
    {
        public int Id { get; set; }

        public string RoleId { get; set; } = string.Empty;
        public virtual RoleIdentity RoleIdentity { get; set; } = null!;

        public int AbilityId { get; set; }
        public virtual Ability Ability { get; set; } = null!;
    }
}
