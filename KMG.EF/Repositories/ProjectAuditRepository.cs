using KMG.Core.Interfaces;
using KMG.Core.Models;
using KMG.EF.Data;

namespace KMG.EF.Repositories
{
    public class ProjectAuditRepository : BaseRepository<ProjectAudit>, IProjectAuditRepository
    {
        public ProjectAuditRepository(ApplicationDbContext context) : base(context) { }
    }
}
