using System;

namespace QLSystem.Licensing
{
    /// <summary>
    /// مسؤول عن كشف محاولات التحايل بإرجاع ساعة وتاريخ الويندوز للخلف
    /// </summary>
    public static class AntiClockTamper
    {
        /// <summary>
        /// يتحقق إذا كانت الساعة الحالية منطقية مقارنة بآخر وقت مسجل
        /// </summary>
        public static bool IsClockManipulated(DateTime now, DateTime lastRecordedTime)
        {
            // إذا كان الوقت الحالي أقل من آخر تشغيل بأكثر من 15 دقيقة، تم التلاعب بالساعة!
            if (now < lastRecordedTime.AddMinutes(-15))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// تشفير التاريخ والوقت في صيغة مشفرة بسيطة للتخزين
        /// </summary>
        public static string EncryptTimestamp(DateTime date)
        {
            var ticks = date.Ticks;
            var obfuscated = ticks ^ 0x5A7E2C91F384B06DL;
            return Convert.ToBase64String(BitConverter.GetBytes(obfuscated));
        }

        /// <summary>
        /// فك تشفير التاريخ والوقت
        /// </summary>
        public static DateTime DecryptTimestamp(string base64)
        {
            try
            {
                var bytes = Convert.FromBase64String(base64);
                var obfuscated = BitConverter.ToInt64(bytes, 0);
                var ticks = obfuscated ^ 0x5A7E2C91F384B06DL;
                return new DateTime(ticks);
            }
            catch
            {
                return DateTime.MinValue;
            }
        }
    }
}
