using KMG.Core.Interfaces;
using KMG.Core.Models;
using KMG.EF.Data;

namespace KMG.EF.Repositories
{
    public class ProjectExpenseRepository : BaseRepository<ProjectExpense>, IProjectExpenseRepository
    {
        public ProjectExpenseRepository(ApplicationDbContext context) : base(context) { }
    }
}
