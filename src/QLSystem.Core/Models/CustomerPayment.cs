using System;
using QLSystem.Core.Enums;

namespace QLSystem.Core.Models
{
    /// <summary>
    /// تسديد دفعة من دين الزبون (Versement client / Remboursement dette)
    /// </summary>
    public class CustomerPayment
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }

        /// <summary>
        /// المبلغ المسدد دج (Montant versé)
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// الرصيد القديم قبل الدفعة
        /// </summary>
        public decimal PreviousDebt { get; set; }

        /// <summary>
        /// الرصيد المتبقي بعد الدفعة
        /// </summary>
        public decimal RemainingDebt { get; set; }

        public PaymentType PaymentMethod { get; set; } = PaymentType.Cash;

        /// <summary>
        /// رقم الوصل أو الشيك إن وجد
        /// </summary>
        public string? ReferenceNumber { get; set; }

        public string? Notes { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.Now;
    }
}
