using KMG.Core.Interfaces;
using KMG.Core.Models;
using KMG.EF.Data;

namespace KMG.EF.Repositories
{
    public class AiChatMessageRepository : BaseRepository<AiChatMessage>, IAiChatMessageRepository
    {
        public AiChatMessageRepository(ApplicationDbContext context) : base(context) { }
    }
}
