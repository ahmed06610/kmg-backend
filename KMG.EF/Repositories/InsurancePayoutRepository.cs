using KMG.Core.Interfaces;
using KMG.Core.Models;
using KMG.EF.Data;

namespace KMG.EF.Repositories
{
    public class InsurancePayoutRepository : BaseRepository<InsurancePayout>, IInsurancePayoutRepository
    {
        public InsurancePayoutRepository(ApplicationDbContext context) : base(context) { }
    }
}
