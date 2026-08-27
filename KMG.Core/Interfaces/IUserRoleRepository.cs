using Microsoft.AspNetCore.Identity;

namespace KMG.Core.Interfaces
{
    public interface IUserRoleRepository : IBaseRepository<IdentityUserRole<string>>
    {
        Task<List<string>> GetUserRoleIdsAsync(string userId);
    }
}
