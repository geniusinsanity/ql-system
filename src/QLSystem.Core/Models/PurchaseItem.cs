using QLSystem.Core.Enums;

namespace QLSystem.Core.Models
{
    /// <summary>
    /// سطر من فاتورة الشراء (Ligne d'achat)
    /// </summary>
    public class PurchaseItem
    {
        public int Id { get; set; }
        public int PurchaseId { get; set; }
        public int ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public UnitType Unit { get; set; } = UnitType.Piece;

        /// <summary>
        /// سعر الشراء للوحدة
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// مجموع السطر
        /// </summary>
        public decimal TotalPrice { get; set; }
    }
}
