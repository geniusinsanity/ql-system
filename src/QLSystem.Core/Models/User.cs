using System;
using QLSystem.Core.Enums;

namespace QLSystem.Core.Models
{
    /// <summary>
    /// حساب مستخدم في البرنامج (Compte utilisateur)
    /// </summary>
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.Cashier;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
