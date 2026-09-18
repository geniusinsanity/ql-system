using System;
using System.Text;
using QLSystem.Core.Models;

namespace QLSystem.Hardware
{
    /// <summary>
    /// مسؤول عن توليد الباركود الداخلي وطباعة تيكيات لاصقة للسلع السائبة (Vis, Écrous, Raccords)
    /// </summary>
    public static class BarcodeService
    {
        /// <summary>
        /// توليد باركود داخلي قياسي EAN-13 للسلع التي لا تملك باركود مصنعي
        /// يبدأ بالبادئة 200 (المعترف بها دولياً للسلع الداخلية داخل المتجر)
        /// </summary>
        public static string GenerateInternalBarcode(int productId)
        {
            // البادئة 200 + 9 أرقام لمعرف السلعة
            var base12 = $"200{productId:D9}";
            var checkDigit = CalculateEan13CheckDigit(base12);
            return base12 + checkDigit;
        }

        /// <summary>
        /// حساب رقم المراقبة الأخير (Check Digit) للـ EAN-13
        /// </summary>
        public static int CalculateEan13CheckDigit(string first12Digits)
        {
            int sum = 0;
            for (int i = 0; i < 12; i++)
            {
                int digit = first12Digits[i] - '0';
                sum += (i % 2 == 0) ? digit : digit * 3;
            }
            int mod = sum % 10;
            return (mod == 0) ? 0 : 10 - mod;
        }

        /// <summary>
        /// توليد تيكات لاصقة (Format Étiquette) للطباعة على طابعة ملصقات الباركود (Xprinter / Zebra)
        /// </summary>
        public static string FormatLabelContent(Product product, StoreSettings settings)
        {
            var sb = new StringBuilder();
            sb.AppendLine("--------------------------------");
            sb.AppendLine(settings.StoreName);
            sb.AppendLine(product.Name);
            if (!string.IsNullOrEmpty(product.Dimensions))
            {
                sb.AppendLine($"Réf: {product.Reference} - {product.Dimensions}");
            }
            else
            {
                sb.AppendLine($"Réf: {product.Reference}");
            }
            sb.AppendLine($"Code: {product.Barcode}");
            sb.AppendLine($"PRIX: {product.SalePrice:N0} DZD");
            sb.AppendLine("--------------------------------");
            return sb.ToString();
        }
    }
}
