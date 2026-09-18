using System;
using QLSystem.Core.Enums;

namespace QLSystem.Core.Models
{
    /// <summary>
    /// سلعة الكانكري (Article de Quincaillerie)
    /// </summary>
    public class Product
    {
        public int Id { get; set; }

        /// <summary>
        /// رمز السلعة الداخلي (Référence: VIS-440, COUD-32, CAB-15...)
        /// </summary>
        public string Reference { get; set; } = string.Empty;

        /// <summary>
        /// كود الباركود (Code-barres EAN-13 / Code-128)
        /// </summary>
        public string? Barcode { get; set; }

        /// <summary>
        /// تسمية السلعة بالفرنسية (Nom de l'article)
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// تسمية السلعة بالعربية
        /// </summary>
        public string? NameAr { get; set; }

        /// <summary>
        /// الفئة (Catégorie)
        /// </summary>
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }

        /// <summary>
        /// القياسات / الأبعاد (Dimensions: 4x40, M6, Ø32, 2.5mm²...)
        /// </summary>
        public string? Dimensions { get; set; }

        /// <summary>
        /// العلامة التجارية (Marque: Total, Ingco, Bticino, Somap...)
        /// </summary>
        public string? Brand { get; set; }

        /// <summary>
        /// سعر الشراء دج (Prix d'achat)
        /// </summary>
        public decimal PurchasePrice { get; set; }

        /// <summary>
        /// سعر البيع بالتجزئة دج (Prix de vente détail)
        /// </summary>
        public decimal SalePrice { get; set; }

        /// <summary>
        /// سعر البيع بالجملة / للمقاولين والحرفيين دج (Prix de gros / Artisans)
        /// </summary>
        public decimal WholesalePrice { get; set; }

        /// <summary>
        /// الكمية الحالية في المخزن (Stock actuel)
        /// </summary>
        public decimal StockQuantity { get; set; }

        /// <summary>
        /// حد التنبيه لنقص المخزون (Stock minimum / Seuil d'alerte)
        /// </summary>
        public decimal MinStockAlert { get; set; } = 5;

        /// <summary>
        /// وحدة البيع بالتجزئة (Pièce, Mètre, Kg...)
        /// </summary>
        public UnitType SaleUnit { get; set; } = UnitType.Piece;

        /// <summary>
        /// وحدة الشراء بالجملة (Carton, Rouleau, Boîte...)
        /// </summary>
        public UnitType PurchaseUnit { get; set; } = UnitType.Piece;

        /// <summary>
        /// معامل التحويل (كم حبة تجزئة داخل وحدة الشراء بالجملة)
        /// مثال: 1 كرتونة = 200 حبة -> ConversionFactor = 200
        /// </summary>
        public decimal ConversionFactor { get; set; } = 1;

        /// <summary>
        /// مكان تواجد السلعة في المحل (Rayon, Étagère, Tiroir...)
        /// </summary>
        public string? Location { get; set; }

        /// <summary>
        /// المورد المفضل (Fournisseur habituel)
        /// </summary>
        public int? DefaultSupplierId { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// هل المخزون ضعيف وفي حالة خطر؟
        /// </summary>
        public bool IsLowStock => StockQuantity <= MinStockAlert;
    }
}
