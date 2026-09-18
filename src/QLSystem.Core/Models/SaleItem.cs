using QLSystem.Core.Enums;

namespace QLSystem.Core.Models
{
    /// <summary>
    /// سطر من فاتورة البيع (Ligne de vente)
    /// </summary>
    public class SaleItem
    {
        public int Id { get; set; }
        public int SaleId { get; set; }
        public int ProductId { get; set; }

        public string ProductReference { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;

        /// <summary>
        /// الكمية المباعة
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// الوحدة المستعملة في البيع
        /// </summary>
        public UnitType Unit { get; set; } = UnitType.Piece;

        /// <summary>
        /// سعر بيع الوحدة الواحدة
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// سعر شراء الوحدة لحساب الفائدة
        /// </summary>
        public decimal PurchasePrice { get; set; }

        /// <summary>
        /// مجموع السطر (Quantity * UnitPrice)
        /// </summary>
        public decimal TotalPrice { get; set; }

        /// <summary>
        /// ربح هذا السطر ((UnitPrice - PurchasePrice) * Quantity)
        /// </summary>
        public decimal LineProfit => (UnitPrice - PurchasePrice) * Quantity;
    }
}
