namespace QLSystem.Core.Enums
{
    /// <summary>
    /// رتبة المستخدم في المحل (Rôle utilisateur)
    /// </summary>
    public enum UserRole
    {
        /// <summary>
        /// مسؤول كامل الصلاحيات - صاحب المحل (Administrateur)
        /// </summary>
        Admin = 0,

        /// <summary>
        /// بائع في الكاسة فقط - بدون صلاحيات التعديل أو رؤية الأرباح (Caissier)
        /// </summary>
        Cashier = 1
    }
}
