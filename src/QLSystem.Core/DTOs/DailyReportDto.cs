using System;

namespace QLSystem.Core.DTOs
{
    /// <summary>
    /// تقرير ملخص اليوم (Rapport de clôture journalière)
    /// </summary>
    public class DailyReportDto
    {
        public DateTime Date { get; set; } = DateTime.Today;

        /// <summary>
        /// إجمالي رقم الأعمال اليوم دج (Chiffre d'affaires)
        /// </summary>
        public decimal TotalSales { get; set; }

        /// <summary>
        /// مجموع المقبوضات كاش دج (Total Espèces encaissé)
        /// </summary>
        public decimal CashCollected { get; set; }

        /// <summary>
        /// مجموع مبيعات الكريدي اليوم دج (Total Crédit accordé)
        /// </summary>
        public decimal CreditGiven { get; set; }

        /// <summary>
        /// ديون تم تسديدها اليوم من الزبائن دج (Remboursements reçus)
        /// </summary>
        public decimal DebtsCollected { get; set; }

        /// <summary>
        /// صافي الأرباح المقدرة لليوم دج (Bénéfice net estimé)
        /// </summary>
        public decimal NetProfit { get; set; }

        /// <summary>
        /// عدد عمليات البيع المنجزة
        /// </summary>
        public int TotalTransactions { get; set; }

        /// <summary>
        /// عدد السلع التي أوشكت على النفاد
        /// </summary>
        public int LowStockAlertsCount { get; set; }
    }
}
