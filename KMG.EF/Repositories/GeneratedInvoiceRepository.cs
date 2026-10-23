using KMG.Core.Interfaces;
using KMG.Core.Models;
using KMG.EF.Data;

namespace KMG.EF.Repositories
{
    public class GeneratedInvoiceRepository : BaseRepository<GeneratedInvoice>, IGeneratedInvoiceRepository
    {
        public GeneratedInvoiceRepository(ApplicationDbContext context) : base(context) { }
    }
}
