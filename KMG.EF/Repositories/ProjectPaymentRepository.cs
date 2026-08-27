using KMG.Core.Interfaces;
using KMG.Core.Models;
using KMG.EF.Data;

namespace KMG.EF.Repositories
{
    public class ProjectPaymentRepository : BaseRepository<ProjectPayment>, IProjectPaymentRepository
    {
        public ProjectPaymentRepository(ApplicationDbContext context) : base(context) { }
    }
}
