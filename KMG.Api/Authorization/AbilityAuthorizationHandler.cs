using System.Security.Claims;
using KMG.Core.Authorization;
using KMG.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace KMG.Api.Authorization
{
    public class AbilityAuthorizationHandler : AuthorizationHandler<AbilityRequirement>
    {
        private readonly IUnitOfWork _unitOfWork;

        public AbilityAuthorizationHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            AbilityRequirement requirement)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return;

            var userAbilities = await _unitOfWork.UserAbility
                .GetQueryable(u => u.UserId == userId)
                .Include(u => u.Ability)
                .Select(u => u.Ability.AbilityName)
                .ToListAsync();

            var userRoles = await _unitOfWork.UserRole
                .GetQueryable(ur => ur.UserId == userId)
                .Select(ur => ur.RoleId)
                .ToListAsync();

            var roleAbilities = await _unitOfWork.RolesAbility
                .GetQueryable(ra => userRoles.Contains(ra.RoleId))
                .Include(ra => ra.Ability)
                .Select(ra => ra.Ability.AbilityName)
                .ToListAsync();

            var allAbilities = userAbilities.Union(roleAbilities).ToList();

            if (allAbilities.Contains(requirement.AbilityName))
                context.Succeed(requirement);
        }
    }
}
