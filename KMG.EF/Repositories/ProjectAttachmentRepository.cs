using KMG.Core.Interfaces;
using KMG.Core.Models;
using KMG.EF.Data;

namespace KMG.EF.Repositories
{
    public class ProjectAttachmentRepository : BaseRepository<ProjectAttachment>, IProjectAttachmentRepository
    {
        public ProjectAttachmentRepository(ApplicationDbContext context) : base(context) { }
    }
}
