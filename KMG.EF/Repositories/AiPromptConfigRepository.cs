using KMG.Core.Interfaces;
using KMG.Core.Models;
using KMG.EF.Data;

namespace KMG.EF.Repositories
{
    public class AiPromptConfigRepository : BaseRepository<AiPromptConfig>, IAiPromptConfigRepository
    {
        public AiPromptConfigRepository(ApplicationDbContext context) : base(context) { }
    }
}
