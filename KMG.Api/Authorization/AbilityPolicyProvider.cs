using KMG.Core.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace KMG.Api.Authorization
{
    /// <summary>
    /// بيبني AuthorizationPolicy لأي اسم يبدأ بـ "Ability:" ديناميكيًا وقت الطلب نفسه،
    /// بدل ما يتبنوا مرة واحدة وقت إقلاع التطبيق من قاعدة بيانات ممكن تكون لسه فاضية
    /// (قبل ما الـ Migration/Seed يشتغلوا). الفحص الفعلي بيفضل في AbilityAuthorizationHandler
    /// اللي بيقرا صلاحيات المستخدم من الداتابيز في كل Request أصلًا.
    /// </summary>
    public class AbilityPolicyProvider : IAuthorizationPolicyProvider
    {
        private const string Prefix = "Ability:";
        private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;

        public AbilityPolicyProvider(IOptions<AuthorizationOptions> options)
        {
            _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
        }

        public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            if (policyName.StartsWith(Prefix, StringComparison.Ordinal))
            {
                var abilityName = policyName[Prefix.Length..];
                var policy = new AuthorizationPolicyBuilder()
                    .AddRequirements(new AbilityRequirement(abilityName))
                    .Build();
                return Task.FromResult<AuthorizationPolicy?>(policy);
            }

            return _fallbackPolicyProvider.GetPolicyAsync(policyName);
        }

        public Task<AuthorizationPolicy?> GetDefaultPolicyAsync() => _fallbackPolicyProvider.GetDefaultPolicyAsync();

        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallbackPolicyProvider.GetFallbackPolicyAsync();
    }
}
