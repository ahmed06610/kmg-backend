using KMG.Core.Interfaces;
using KMG.Core.Models;
using KMG.EF.Data;

namespace KMG.EF.Repositories
{
    public class AiTenderResultRepository : BaseRepository<AiTenderResult>, IAiTenderResultRepository
    {
        public AiTenderResultRepository(ApplicationDbContext context) : base(context) { }
    }
}
