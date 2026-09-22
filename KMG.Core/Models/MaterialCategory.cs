namespace KMG.Core.Models
{
    public class MaterialCategory
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<CategoryFieldDefinition> ExtraFieldDefinitions { get; set; } = new();

        public virtual ICollection<Material> Materials { get; set; } = new List<Material>();
    }
}
