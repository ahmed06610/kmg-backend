using System.ComponentModel.DataAnnotations.Schema;

namespace KMG.Core.Models
{
    // صف واحد فقط للشركة كلها (Singleton) - الخزنة المركزية
    public class CashBox
    {
        public int Id { get; set; }
        public decimal TotalCash { get; set; }
        public decimal TotalCredit { get; set; }

        [NotMapped]
        public decimal TotalBalance => TotalCash + TotalCredit;

        public virtual ICollection<CashBoxTransaction> Transactions { get; set; } = new List<CashBoxTransaction>();
    }
}
