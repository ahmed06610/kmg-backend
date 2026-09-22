namespace KMG.Core.DTOs.Client
{
    public class ClientListDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public int ProjectsCount { get; set; }
        public decimal TotalContractValue { get; set; }
        public decimal TotalCollected { get; set; }
        public decimal TotalRemaining { get; set; }
    }

    public class ClientDetailsDTO : ClientListDTO
    {
        public string? Address { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<ClientProjectSummaryDTO> Projects { get; set; } = new();
    }

    public class ClientProjectSummaryDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ProjectCode { get; set; } = string.Empty;
        public string ProjectType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal ContractValue { get; set; }
        public decimal TotalCollected { get; set; }
        public decimal RemainingBalance { get; set; }
    }

    public class CreateClientDTO
    {
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
    }

    public class UpdateClientDTO : CreateClientDTO
    {
        public int Id { get; set; }
    }
}
