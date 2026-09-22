using KMG.Core.Interfaces;
using KMG.Core.Models;
using KMG.EF.Data;

namespace KMG.EF.Repositories
{
    public class MaterialCategoryRepository : BaseRepository<MaterialCategory>, IMaterialCategoryRepository
    {
        public MaterialCategoryRepository(ApplicationDbContext context) : base(context) { }
    }
}
