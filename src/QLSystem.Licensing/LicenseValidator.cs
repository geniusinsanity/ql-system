using System;
using System.Security.Cryptography;
using System.Text;

namespace QLSystem.Licensing
{
    /// <summary>
    /// مسؤول عن فحص وتأكيد صحة كود التفعيل الدائم
    /// </summary>
    public static class LicenseValidator
    {
        // المفتاح السري الخاص بالمطور فقط لتوقيع المفاتيح
        internal const string DeveloperSecret = "QL_SECRET_DEVELOPER_KEY_ALGERIA_QUINCAILLERIE_2026_X99";

        /// <summary>
        /// التحقق هل المفتاح المدخل يطابق بصمة جهاز الزبون
        /// </summary>
        public static bool ValidateKey(string machineId, string enteredKey)
        {
            if (string.IsNullOrWhiteSpace(machineId) || string.IsNullOrWhiteSpace(enteredKey))
            {
                return false;
            }

            var cleanKey = enteredKey.Trim().ToUpperInvariant();
            var expectedKey = GenerateKeyForMachine(machineId);

            return string.Equals(cleanKey, expectedKey, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// توليد كود التفعيل المطابق لبصمة الجهاز
        /// صيغة الكود: ACT-XXXX-XXXX-XXXX-XXXX
        /// </summary>
        public static string GenerateKeyForMachine(string machineId)
        {
            var cleanMachineId = machineId.Trim().ToUpperInvariant();

            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(DeveloperSecret)))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(cleanMachineId));
                var hex = BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();

                var p1 = hex.Substring(0, 4);
                var p2 = hex.Substring(4, 4);
                var p3 = hex.Substring(8, 4);
                var p4 = hex.Substring(12, 4);

                return $"ACT-{p1}-{p2}-{p3}-{p4}";
            }
        }
    }
}
