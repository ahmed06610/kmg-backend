using KMG.Core.DTOs.Client;

namespace KMG.Core.Interfaces.Services
{
    public interface IClientService
    {
        Task<List<ClientListDTO>> GetAllAsync();
        Task<ClientDetailsDTO?> GetByIdAsync(int id);
        Task<int> CreateAsync(CreateClientDTO model);
        Task<bool> UpdateAsync(UpdateClientDTO model);
        Task<bool> DeleteAsync(int id);
    }
}
