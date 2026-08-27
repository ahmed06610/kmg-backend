namespace KMG.Core.Helper
{
    public class JWT
    {
        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int DurationInDays { get; set; }
    }
}
