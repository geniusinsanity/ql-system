using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using QLSystem.Core.Models;

namespace QLSystem.Hardware
{
    /// <summary>
    /// مسؤول عن تجهيز وتنسيق أوامر التذاكر الحرارية 80mm و 58mm بلغة ESC/POS المباشرة
    /// </summary>
    public class EscPosPrinter
    {
        private readonly int _widthChars; // 48 chars for 80mm, 32 chars for 58mm

        public EscPosPrinter(int paperWidthMm = 80)
        {
            _widthChars = paperWidthMm <= 58 ? 32 : 48;
        }

        /// <summary>
        /// توليد تيار بايتات التذكرة الحرارية الكاملة لعملية بيع
        /// </summary>
        public byte[] GenerateReceiptBytes(Sale sale, StoreSettings settings)
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms, Encoding.GetEncoding("windows-1256"))) // Arabic / Latin code page
            {
                // 1. تهيئة الطابعة (ESC @)
                bw.Write(new byte[] { 0x1B, 0x40 });

                // 2. رأس التذكرة في المنتصف (ESC a 1)
                SetAlignment(bw, 1);
                SetBold(bw, true);
                bw.Write(Encoding.UTF8.GetBytes(CenterText(settings.StoreName) + "\n"));
                SetBold(bw, false);

                if (!string.IsNullOrEmpty(settings.Phone1))
                {
                    bw.Write(Encoding.UTF8.GetBytes(CenterText($"Tél: {settings.Phone1}") + "\n"));
                }
                if (!string.IsNullOrEmpty(settings.Address))
                {
                    bw.Write(Encoding.UTF8.GetBytes(CenterText(settings.Address) + "\n"));
                }

                bw.Write(Encoding.UTF8.GetBytes(CenterText(settings.ReceiptHeader) + "\n"));
                bw.Write(Encoding.UTF8.GetBytes(new string('-', _widthChars) + "\n"));

                // 3. معلومات الفاتورة
                SetAlignment(bw, 0); // يسار
                bw.Write(Encoding.UTF8.GetBytes($"N°: {sale.InvoiceNumber}\n"));
                if (!string.IsNullOrWhiteSpace(sale.CustomerName) && !sale.CustomerName.ToLower().Contains("comptoir") && !sale.CustomerName.Contains("زبون عادي"))
                {
                    bw.Write(Encoding.UTF8.GetBytes($"Client: {sale.CustomerName}\n"));
                }
                bw.Write(Encoding.UTF8.GetBytes(new string('-', _widthChars) + "\n"));

                // 4. رأس جدول السلع
                if (_widthChars >= 48)
                {
                    bw.Write(Encoding.UTF8.GetBytes("Article                  Qté   P.U      Total\n"));
                }
                else
                {
                    bw.Write(Encoding.UTF8.GetBytes("Article           Qté   Total\n"));
                }
                bw.Write(Encoding.UTF8.GetBytes(new string('-', _widthChars) + "\n"));

                // 5. عناصر الفاتورة
                foreach (var item in sale.Items)
                {
                    var name = item.ProductName.Length > 20 ? item.ProductName.Substring(0, 19) + "." : item.ProductName;
                    string line;
                    if (_widthChars >= 48)
                    {
                        line = $"{name,-22} {item.Quantity,4} {item.UnitPrice,7:N0} {item.TotalPrice,8:N0}\n";
                    }
                    else
                    {
                        line = $"{name,-16} {item.Quantity,3} {item.TotalPrice,7:N0}\n";
                    }
                    bw.Write(Encoding.UTF8.GetBytes(line));
                }

                bw.Write(Encoding.UTF8.GetBytes(new string('=', _widthChars) + "\n"));

                // 6. المجاميع والدفع
                SetBold(bw, true);
                var totalLine = FormatTwoColumn("TOTAL:", $"{sale.TotalAmount:N2} DZD");
                bw.Write(Encoding.UTF8.GetBytes(totalLine + "\n"));
                SetBold(bw, false);

                if (sale.Discount > 0)
                {
                    bw.Write(Encoding.UTF8.GetBytes(FormatTwoColumn("Remise:", $"{sale.Discount:N2} DZD") + "\n"));
                }

                bw.Write(Encoding.UTF8.GetBytes(FormatTwoColumn("Versement (Espèces):", $"{sale.PaidAmount:N2} DZD") + "\n"));

                if (sale.ChangeAmount > 0)
                {
                    bw.Write(Encoding.UTF8.GetBytes(FormatTwoColumn("Rendu:", $"{sale.ChangeAmount:N2} DZD") + "\n"));
                }

                if (sale.DebtAmount > 0)
                {
                    SetBold(bw, true);
                    bw.Write(Encoding.UTF8.GetBytes(FormatTwoColumn("RESTE CRÉDIT:", $"{sale.DebtAmount:N2} DZD") + "\n"));
                    SetBold(bw, false);
                }

                // 7. أسفل التذكرة
                bw.Write(Encoding.UTF8.GetBytes(new string('-', _widthChars) + "\n"));
                SetAlignment(bw, 1);
                bw.Write(Encoding.UTF8.GetBytes(CenterText(settings.ReceiptFooter) + "\n\n"));

                // 8. قص الورقة (GS V 66 0)
                bw.Write(new byte[] { 0x1D, 0x56, 0x42, 0x00 });

                return ms.ToArray();
            }
        }

        private static void SetAlignment(BinaryWriter bw, byte align)
        {
            // ESC a n (0: Left, 1: Center, 2: Right)
            bw.Write(new byte[] { 0x1B, 0x61, align });
        }

        private static void SetBold(BinaryWriter bw, bool bold)
        {
            // ESC E n (1: Bold on, 0: Bold off)
            bw.Write(new byte[] { 0x1B, 0x45, (byte)(bold ? 1 : 0) });
        }

        private string CenterText(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            if (text.Length >= _widthChars) return text;
            var pad = (_widthChars - text.Length) / 2;
            return new string(' ', pad) + text;
        }

        private string FormatTwoColumn(string left, string right)
        {
            var spaces = _widthChars - left.Length - right.Length;
            if (spaces < 1) spaces = 1;
            return left + new string(' ', spaces) + right;
        }
    }
}
