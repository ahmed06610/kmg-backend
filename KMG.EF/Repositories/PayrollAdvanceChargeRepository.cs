using KMG.Core.Interfaces;
using KMG.Core.Models;
using KMG.EF.Data;

namespace KMG.EF.Repositories
{
    public class PayrollAdvanceChargeRepository : BaseRepository<PayrollAdvanceCharge>, IPayrollAdvanceChargeRepository
    {
        public PayrollAdvanceChargeRepository(ApplicationDbContext context) : base(context) { }
    }
}
