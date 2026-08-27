using Microsoft.AspNetCore.Authorization;

namespace KMG.Core.Authorization
{
    public class AbilityRequirement : IAuthorizationRequirement
    {
        public string AbilityName { get; }

        public AbilityRequirement(string abilityName)
        {
            AbilityName = abilityName;
        }
    }
}
