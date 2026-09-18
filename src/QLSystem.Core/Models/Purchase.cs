using System;
using System.Collections.Generic;

namespace QLSystem.Core.Models
{
    /// <summary>
    /// عملية شراء وسلعة داخلة للمخزن (Achat fournisseur / Entrée de stock)
    /// </summary>
    public class Purchase
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public int SupplierId { get; set; }
        public string? SupplierName { get; set; }

        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingDebt { get; set; }

        public string? Notes { get; set; }
        public DateTime PurchaseDate { get; set; } = DateTime.Now;

        public List<PurchaseItem> Items { get; set; } = new List<PurchaseItem>();
    }
}
