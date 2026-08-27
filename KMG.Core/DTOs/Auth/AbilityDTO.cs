namespace KMG.Core.DTOs.Auth
{
    public class AbilityDTO
    {
        public int Id { get; set; }
        public string Label { get; set; } = string.Empty;
        public string? Href { get; set; }
    }

    public class AbilitySelectionDTO
    {
        public int Id { get; set; }
        public string AbilityName { get; set; } = string.Empty;
        public bool IsRelated { get; set; }
    }
}
