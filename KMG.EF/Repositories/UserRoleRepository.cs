using KMG.Core.Interfaces;
using KMG.EF.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KMG.EF.Repositories
{
    public class UserRoleRepository : BaseRepository<IdentityUserRole<string>>, IUserRoleRepository
    {
        public UserRoleRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<string>> GetUserRoleIdsAsync(string userId)
        {
            return await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.RoleId)
                .ToListAsync();
        }
    }
}
