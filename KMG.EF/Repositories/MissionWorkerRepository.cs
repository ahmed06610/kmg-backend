using KMG.Core.Interfaces;
using KMG.Core.Models;
using KMG.EF.Data;

namespace KMG.EF.Repositories
{
    public class MissionWorkerRepository : BaseRepository<MissionWorker>, IMissionWorkerRepository
    {
        public MissionWorkerRepository(ApplicationDbContext context) : base(context) { }
    }
}
