using System;
using System.Collections.Generic;
using QLSystem.Core.Enums;

namespace QLSystem.Core.Models
{
    /// <summary>
    /// عملية البيع / الفاتورة (Vente / Facture / Bon de caisse)
    /// </summary>
    public class Sale
    {
        public int Id { get; set; }

        /// <summary>
        /// رقم الفاتورة أو التذكرة الفريد (N° Ticket / Facture)
        /// </summary>
        public string InvoiceNumber { get; set; } = string.Empty;

        /// <summary>
        /// معرف الزبون (أو فارغ إذا كان زبون عادي غير مسجل)
        /// </summary>
        public int? CustomerId { get; set; }
        public string CustomerName { get; set; } = "زبون عادي (Comptoir)";

        public PaymentType PaymentType { get; set; } = PaymentType.Cash;

        /// <summary>
        /// المجموع قبل التخفيض
        /// </summary>
        public decimal SubTotal { get; set; }

        /// <summary>
        /// قيمة التخفيض دج (Remise)
        /// </summary>
        public decimal Discount { get; set; }

        /// <summary>
        /// المجموع الصافي الواجب دفعه
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// المبلغ المدفوع كاش
        /// </summary>
        public decimal PaidAmount { get; set; }

        /// <summary>
        /// الصرف المرجع للزبون (Rendu monnaie)
        /// </summary>
        public decimal ChangeAmount { get; set; }

        /// <summary>
        /// المبلغ المتبقي كدين على الزبون
        /// </summary>
        public decimal DebtAmount { get; set; }

        /// <summary>
        /// صافي ربح هذه العملية (Bénéfice net)
        /// </summary>
        public decimal ProfitAmount { get; set; }

        /// <summary>
        /// المستخدم الذي أجرى عملية البيع
        /// </summary>
        public int UserId { get; set; }
        public string? UserName { get; set; }

        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public List<SaleItem> Items { get; set; } = new List<SaleItem>();
    }
}
