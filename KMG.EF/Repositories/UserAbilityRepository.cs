using KMG.Core.Interfaces;
using KMG.Core.Models;
using KMG.EF.Data;

namespace KMG.EF.Repositories
{
    public class UserAbilityRepository : BaseRepository<UserAbility>, IUserAbilityRepository
    {
        public UserAbilityRepository(ApplicationDbContext context) : base(context) { }
    }
}
