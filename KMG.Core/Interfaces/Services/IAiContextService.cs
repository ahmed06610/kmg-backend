using KMG.Core.DTOs.AiContext;

namespace KMG.Core.Interfaces.Services
{
    public interface IAiContextService
    {
        Task<AiPromptDTO> GetPromptAsync();
        Task<AiPromptDTO> UpdatePromptAsync(string promptText, int employeeId);
        Task<AiContextResponseDTO> GetContextDataAsync();

        Task<AiTenderResultDTO> IngestTenderResultAsync(CreateAiTenderResultDTO model);
        Task<List<AiTenderResultDTO>> IngestTenderResultsAsync(List<CreateAiTenderResultDTO> matches);
        Task<List<AiTenderResultDTO>> GetActiveTenderResultsAsync();
        Task<AiTenderResultDTO?> GetTenderResultByIdAsync(int id);
        Task DismissTenderResultAsync(int id);
    }
}
