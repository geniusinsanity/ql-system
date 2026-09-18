using System;

namespace QLSystem.Core.Models
{
    /// <summary>
    /// إعدادات المحل والطباعة (Paramètres de la quincaillerie)
    /// </summary>
    public class StoreSettings
    {
        public int Id { get; set; } = 1;

        /// <summary>
        /// اسم المحل (Ex: Quincaillerie El-Baraka)
        /// </summary>
        public string StoreName { get; set; } = "Quincaillerie El-Baraka";

        /// <summary>
        /// اسم صاحب المحل أو النشاط
        /// </summary>
        public string? OwnerName { get; set; }

        public string? Phone1 { get; set; } = "0550 00 00 00";
        public string? Phone2 { get; set; }
        public string? Address { get; set; } = "Alger, Algérie";
        public string? RegistreCommerce { get; set; }
        public string? NIF { get; set; }
        public string? NIS { get; set; }

        /// <summary>
        /// رسالة أعلى تذكرة الكاسة
        /// </summary>
        public string ReceiptHeader { get; set; } = "مرحباً بكم - Bienvenue chez nous";

        /// <summary>
        /// رسالة أسفل تذكرة الكاسة
        /// </summary>
        public string ReceiptFooter { get; set; } = "شكراً لزيارتكم - Merci de votre visite";

        /// <summary>
        /// اسم الطابعة الحرارية الافتراضية
        /// </summary>
        public string? ThermalPrinterName { get; set; }

        /// <summary>
        /// عرض ورق الطابعة بالميليمتر (80mm أو 58mm)
        /// </summary>
        public int ThermalPrinterWidthMm { get; set; } = 80;

        /// <summary>
        /// هل نطبع التذكرة تلقائياً بعد كل عملية بيع؟
        /// </summary>
        public bool AutoPrintReceipt { get; set; } = true;

        /// <summary>
        /// هل نفتح درج النقود تلقائياً بعد كل عملية بيع؟
        /// </summary>
        public bool AutoOpenCashDrawer { get; set; } = true;

        /// <summary>
        /// مسار مجلد النسخ الاحتياطي التلقائي
        /// </summary>
        public string BackupFolder { get; set; } = string.Empty;

        /// <summary>
        /// لغة واجهة البرنامج الافتراضية (ar أو fr)
        /// </summary>
        public string Language { get; set; } = "fr";
    }
}
