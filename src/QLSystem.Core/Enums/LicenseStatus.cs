namespace QLSystem.Core.Enums
{
    /// <summary>
    /// حالة ترخيص البرنامج على حاسوب المحل
    /// </summary>
    public enum LicenseStatus
    {
        /// <summary>
        /// فترة تجريبية سارية (Essai 7 jours actif)
        /// </summary>
        TrialActive = 0,

        /// <summary>
        /// انتهت فترة التجربة ويجب شراء التفعيل (Essai expiré)
        /// </summary>
        TrialExpired = 1,

        /// <summary>
        /// تم التلاعب بساعة الويندوز (Horloge modifiée / Tentative de fraude)
        /// </summary>
        ClockTampered = 2,

        /// <summary>
        /// البرنامج مفعل بصفة دائمة وقانونية (Version activée permanente)
        /// </summary>
        Activated = 3
    }
}
