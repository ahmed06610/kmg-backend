using KMG.Core.Interfaces;
using KMG.Core.Models;
using KMG.EF.Data;

namespace KMG.EF.Repositories
{
    public class MisrInsurancePayoutRepository : BaseRepository<MisrInsurancePayout>, IMisrInsurancePayoutRepository
    {
        public MisrInsurancePayoutRepository(ApplicationDbContext context) : base(context) { }
    }
}
