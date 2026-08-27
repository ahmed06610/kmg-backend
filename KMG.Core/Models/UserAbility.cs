namespace KMG.Core.Models
{
    public class UserAbility
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser User { get; set; } = null!;

        public int AbilityId { get; set; }
        public virtual Ability Ability { get; set; } = null!;
    }
}
