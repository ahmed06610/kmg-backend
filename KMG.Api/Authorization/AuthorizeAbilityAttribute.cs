using Microsoft.AspNetCore.Authorization;

namespace KMG.Api.Authorization
{
    public class AuthorizeAbilityAttribute : AuthorizeAttribute
    {
        public AuthorizeAbilityAttribute(string ability)
        {
            Policy = $"Ability:{ability}";
        }
    }
}
