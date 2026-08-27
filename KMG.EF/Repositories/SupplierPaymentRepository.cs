using KMG.Core.Interfaces;
using KMG.Core.Models;
using KMG.EF.Data;

namespace KMG.EF.Repositories
{
    public class SupplierPaymentRepository : BaseRepository<SupplierPayment>, ISupplierPaymentRepository
    {
        public SupplierPaymentRepository(ApplicationDbContext context) : base(context) { }
    }
}
