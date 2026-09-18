using System;
using QLSystem.Core.Enums;

namespace QLSystem.Core.Models
{
    /// <summary>
    /// معلومات حالة ترخيص البرنامج على هذا الحاسوب
    /// </summary>
    public class LicenseInfo
    {
        /// <summary>
        /// المعرف الفريد لبصمة الجهاز (Machine Fingerprint)
        /// مثال: QL-ALG-7410-9921-X9A2
        /// </summary>
        public string MachineId { get; set; } = string.Empty;

        /// <summary>
        /// مفتاح التفعيل الدائم إن تم شراؤه
        /// </summary>
        public string? ActivationKey { get; set; }

        /// <summary>
        /// تاريخ أول تشغيل للبرنامج (بداية الـ 7 أيام)
        /// </summary>
        public DateTime FirstRunDate { get; set; }

        /// <summary>
        /// تاريخ انتهاء فترة التجربة
        /// </summary>
        public DateTime TrialEndDate { get; set; }

        /// <summary>
        /// آخر تاريخ تم تسجيله لمنع التلاعب بساعة الويندوز
        /// </summary>
        public DateTime LastRunDate { get; set; }

        /// <summary>
        /// الحالة الحالية
        /// </summary>
        public LicenseStatus Status { get; set; } = LicenseStatus.TrialActive;

        /// <summary>
        /// عدد الأيام المتبقية في الفترة التجريبية
        /// </summary>
        public int RemainingDays
        {
            get
            {
                if (Status == LicenseStatus.Activated) return 9999;
                var remaining = (TrialEndDate.Date - DateTime.Now.Date).Days;
                return remaining < 0 ? 0 : remaining;
            }
        }

        /// <summary>
        /// هل البرنامج صالح للعمل حالياً؟
        /// </summary>
        public bool IsUsable => Status == LicenseStatus.Activated || (Status == LicenseStatus.TrialActive && RemainingDays >= 0);
    }
}
