using KMG.Core.Interfaces;
using KMG.Core.Models;
using KMG.EF.Data;

namespace KMG.EF.Repositories
{
    public class PayrollPayoutRepository : BaseRepository<PayrollPayout>, IPayrollPayoutRepository
    {
        public PayrollPayoutRepository(ApplicationDbContext context) : base(context) { }
    }
}
