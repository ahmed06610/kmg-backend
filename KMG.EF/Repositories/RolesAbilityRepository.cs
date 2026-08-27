using KMG.Core.Interfaces;
using KMG.Core.Models;
using KMG.EF.Data;

namespace KMG.EF.Repositories
{
    public class RolesAbilityRepository : BaseRepository<RolesAbility>, IRolesAbilityRepository
    {
        public RolesAbilityRepository(ApplicationDbContext context) : base(context) { }
    }
}
