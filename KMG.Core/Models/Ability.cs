namespace KMG.Core.Models
{
    public class Ability
    {
        public int Id { get; set; }
        public string AbilityName { get; set; } = string.Empty;
        public string? Url { get; set; }

        public virtual ICollection<RolesAbility> RolesAbilities { get; set; } = new List<RolesAbility>();
        public virtual ICollection<UserAbility> UserAbilities { get; set; } = new List<UserAbility>();
    }
}
