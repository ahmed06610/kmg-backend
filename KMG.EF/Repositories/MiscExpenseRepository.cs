using KMG.Core.Interfaces;
using KMG.Core.Models;
using KMG.EF.Data;

namespace KMG.EF.Repositories
{
    public class MiscExpenseRepository : BaseRepository<MiscExpense>, IMiscExpenseRepository
    {
        public MiscExpenseRepository(ApplicationDbContext context) : base(context) { }
    }
}
