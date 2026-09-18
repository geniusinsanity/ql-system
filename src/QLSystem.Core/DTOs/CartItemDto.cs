using System.ComponentModel;
using System.Runtime.CompilerServices;
using QLSystem.Core.Enums;

namespace QLSystem.Core.DTOs
{
    /// <summary>
    /// عنصر سلة البيع في الكاسة (Article dans le panier de caisse)
    /// </summary>
    public class CartItemDto : INotifyPropertyChanged
    {
        private decimal _unitPrice;
        private decimal _quantity = 1;
        private bool _isWholesale;

        public int ProductId { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Dimensions { get; set; } = string.Empty;

        /// <summary>سعر التجزئة الأصلي (يُحفظ دائماً لإمكانية الرجوع)</summary>
        public decimal RetailPrice { get; set; }

        /// <summary>سعر الجملة (0 إذا لم يكن هناك سعر جملة)</summary>
        public decimal WholesalePrice { get; set; }

        /// <summary>السعر الفعلي المطبق حالياً (تجزئة أو جملة)</summary>
        public decimal UnitPrice
        {
            get => _unitPrice;
            set { _unitPrice = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalPrice)); OnPropertyChanged(nameof(Profit)); }
        }

        public decimal PurchasePrice { get; set; }

        public decimal Quantity
        {
            get => _quantity;
            set { _quantity = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalPrice)); OnPropertyChanged(nameof(Profit)); }
        }

        public UnitType Unit { get; set; } = UnitType.Piece;
        public decimal AvailableStock { get; set; }

        /// <summary>هل السعر المطبق هو سعر الجملة؟</summary>
        public bool IsWholesale
        {
            get => _isWholesale;
            set { _isWholesale = value; OnPropertyChanged(); }
        }

        public decimal TotalPrice => Quantity * UnitPrice;
        public decimal Profit => (UnitPrice - PurchasePrice) * Quantity;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
