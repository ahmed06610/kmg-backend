using KMG.Core.Interfaces;
using KMG.Core.Models;
using KMG.EF.Data;

namespace KMG.EF.Repositories
{
    public class AbilityRepository : BaseRepository<Ability>, IAbilityRepository
    {
        public AbilityRepository(ApplicationDbContext context) : base(context) { }
    }
}
