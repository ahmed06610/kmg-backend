using KMG.Core.Interfaces;
using KMG.Core.Models;
using KMG.EF.Data;

namespace KMG.EF.Repositories
{
    public class PayrollAdjustmentRepository : BaseRepository<PayrollAdjustment>, IPayrollAdjustmentRepository
    {
        public PayrollAdjustmentRepository(ApplicationDbContext context) : base(context) { }
    }
}
