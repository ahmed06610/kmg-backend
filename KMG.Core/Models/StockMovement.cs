using KMG.Core.Enums;

namespace KMG.Core.Models
{
    public class StockMovement
    {
        public int Id { get; set; }
        public MovementType MovementType { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPriceAtTime { get; set; }
        public DateTime MovementDate { get; set; }
        public string? Notes { get; set; }

        public int MaterialId { get; set; }
        public virtual Material Material { get; set; } = null!;

        public int? ProjectId { get; set; }
        public virtual Project? Project { get; set; }

        public int? SupplierId { get; set; }
        public virtual Supplier? Supplier { get; set; }

        public int CreatedByEmployeeId { get; set; }
        public virtual Employee CreatedByEmployee { get; set; } = null!;
    }
}
