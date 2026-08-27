using KMG.Core.Interfaces;
using KMG.Core.Models;
using KMG.EF.Data;

namespace KMG.EF.Repositories
{
    public class CashBoxRepository : BaseRepository<CashBox>, ICashBoxRepository
    {
        public CashBoxRepository(ApplicationDbContext context) : base(context) { }
    }
}
