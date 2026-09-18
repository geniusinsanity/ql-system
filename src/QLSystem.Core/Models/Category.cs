using System;

namespace QLSystem.Core.Models
{
    /// <summary>
    /// فئة السلع (Catégorie de produits: Plomberie, Électricité, Visserie...)
    /// </summary>
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? NameAr { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
