using System.ComponentModel.DataAnnotations.Schema;

namespace KMG.Core.Models
{
    public class Material
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal MinimumThreshold { get; set; }
        public DateTime LastUpdated { get; set; }

        public int? CategoryId { get; set; }
        public virtual MaterialCategory? Category { get; set; }
        public Dictionary<string, string> ExtraFieldValues { get; set; } = new();

        [NotMapped]
        public bool IsLowStock => Quantity <= MinimumThreshold;

        [NotMapped]
        public decimal TotalPrice => Quantity * UnitPrice;

        public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    }
}
