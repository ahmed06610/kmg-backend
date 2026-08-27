using KMG.Core.Interfaces;
using KMG.Core.Models;
using KMG.EF.Data;

namespace KMG.EF.Repositories
{
    public class AdvanceRepository : BaseRepository<Advance>, IAdvanceRepository
    {
        public AdvanceRepository(ApplicationDbContext context) : base(context) { }
    }
}
