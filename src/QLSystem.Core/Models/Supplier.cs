using System;

namespace QLSystem.Core.Models
{
    /// <summary>
    /// مورد السلع (Fournisseur de quincaillerie)
    /// </summary>
    public class Supplier
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Wilaya { get; set; }

        /// <summary>
        /// ديننا للمورد دج (Notre dette envers le fournisseur)
        /// </summary>
        public decimal CurrentDebt { get; set; }

        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
