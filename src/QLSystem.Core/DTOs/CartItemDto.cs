using QLSystem.Core.Enums;

namespace QLSystem.Core.DTOs
{
    /// <summary>
    /// عنصر سلة البيع في الكاسة (Article dans le panier de caisse)
    /// </summary>
    public class CartItemDto
    {
        public int ProductId { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal Quantity { get; set; } = 1;
        public UnitType Unit { get; set; } = UnitType.Piece;
        public decimal AvailableStock { get; set; }

        /// <summary>
        /// هل تم اختياره بسعر التجزئة أم الجملة؟
        /// </summary>
        public bool IsWholesale { get; set; }

        public decimal TotalPrice => Quantity * UnitPrice;
        public decimal Profit => (UnitPrice - PurchasePrice) * Quantity;
    }
}
