using System;

namespace QLSystem.Core.Models
{
    /// <summary>
    /// زبون المحل (Client: Particulier, Plombier, Électricien, Maçon, Promoteur)
    /// </summary>
    public class Customer
    {
        public int Id { get; set; }

        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Address { get; set; }

        /// <summary>
        /// صفة الزبون (Ex: Plombier, Électricien, Chantier Bab Ezzouar...)
        /// </summary>
        public string? Activity { get; set; }

        /// <summary>
        /// رصيد الدين الحالي على الزبون دج (Solde dû / Crédit actuel)
        /// </summary>
        public decimal CurrentDebt { get; set; }

        /// <summary>
        /// سقف الدين المسموح به (Plafond de crédit maximum autorisé)
        /// </summary>
        public decimal MaxCreditLimit { get; set; } = 50000;

        /// <summary>
        /// هل الزبون مؤهل لسعر الجملة تلقائياً؟
        /// </summary>
        public bool AppliesWholesalePrice { get; set; } = false;

        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
