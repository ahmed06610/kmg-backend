using KMG.Core.Interfaces;
using KMG.Core.Models;
using KMG.EF.Data;

namespace KMG.EF.Repositories
{
    public class MissionRepository : BaseRepository<Mission>, IMissionRepository
    {
        public MissionRepository(ApplicationDbContext context) : base(context) { }
    }
}
