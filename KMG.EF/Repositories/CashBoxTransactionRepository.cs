using KMG.Core.Interfaces;
using KMG.Core.Models;
using KMG.EF.Data;

namespace KMG.EF.Repositories
{
    public class CashBoxTransactionRepository : BaseRepository<CashBoxTransaction>, ICashBoxTransactionRepository
    {
        public CashBoxTransactionRepository(ApplicationDbContext context) : base(context) { }
    }
}
