using KMG.Core.Interfaces;
using KMG.Core.Models;
using KMG.EF.Data;

namespace KMG.EF.Repositories
{
    public class CustodyRepository : BaseRepository<Custody>, ICustodyRepository
    {
        public CustodyRepository(ApplicationDbContext context) : base(context) { }
    }
}
