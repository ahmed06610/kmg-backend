namespace KMG.Core.DTOs.AiContext
{
    public class AiPromptDTO
    {
        public string PromptText { get; set; } = string.Empty;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedByEmployeeName { get; set; }
    }

    public class UpdateAiPromptDTO
    {
        public string PromptText { get; set; } = string.Empty;
    }

    public class AiCompanyOverviewDTO
    {
        public int TotalProjects { get; set; }
        public Dictionary<string, int> ProjectsByType { get; set; } = new();
        public Dictionary<string, int> ProjectsByStatus { get; set; } = new();
        public int TotalClients { get; set; }
        public int TotalSuppliers { get; set; }
        public int TotalMaterialTypes { get; set; }
    }

    public class AiProjectSummaryDTO
    {
        public string Name { get; set; } = string.Empty;
        public string ProjectType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class AiMaterialCategorySummaryDTO
    {
        public string CategoryName { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public List<string> SampleMaterialNames { get; set; } = new();
    }

    public class AiContextResponseDTO
    {
        public DateTime GeneratedAt { get; set; }
        public string DynamicPrompt { get; set; } = string.Empty;
        public AiCompanyOverviewDTO CompanyOverview { get; set; } = new();
        public List<AiProjectSummaryDTO> Projects { get; set; } = new();
        public List<AiMaterialCategorySummaryDTO> MaterialCategories { get; set; } = new();
    }
}
