using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace QLSystem.Licensing
{
    /// <summary>
    /// مسؤول عن توليد بصمة عتاد فريدة وغير قابلة للتكرار لحاسوب المحل (Machine ID)
    /// </summary>
    public static class HardwareFingerprint
    {
        private const string Salt = "QL_QUINCAILLERIE_SYSTEM_DZ_2026_SECURE_SALT";

        /// <summary>
        /// استخراج المعرف الفريد للجهاز بصيغة مقروءة ومرتبة
        /// مثال: QL-ALG-7F8A-31B9-E04C
        /// </summary>
        public static string GetMachineId()
        {
            try
            {
                var rawIdentifier = CollectSystemHardwareIdentifiers();
                return FormatAsMachineId(rawIdentifier);
            }
            catch
            {
                // Fallback احتياطي في حال عدم توفر الصلاحيات
                var fallback = Environment.MachineName + Environment.UserName + Environment.OSVersion;
                return FormatAsMachineId(fallback);
            }
        }

        private static string CollectSystemHardwareIdentifiers()
        {
            var sb = new StringBuilder();

            // 1. اسم الجهاز واسم المستخدم
            sb.Append(Environment.MachineName).Append("|");
            sb.Append(Environment.OSVersion).Append("|");

            // 2. فحص Windows Registry للـ MachineGuid
            if (OperatingSystemIsWindows())
            {
                try
                {
                    using (var rk = Microsoft.Win32.RegistryKey.OpenBaseKey(
                        Microsoft.Win32.RegistryHive.LocalMachine, Microsoft.Win32.RegistryView.Registry64))
                    {
                        using (var subKey = rk.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography"))
                        {
                            var guid = subKey?.GetValue("MachineGuid")?.ToString();
                            if (!string.IsNullOrEmpty(guid))
                            {
                                sb.Append(guid).Append("|");
                            }
                        }
                    }
                }
                catch
                {
                    // تجاهل والاعتماد على باقي المعرفات
                }
            }
            else
            {
                // على Linux / Fallback
                if (File.Exists("/etc/machine-id"))
                {
                    sb.Append(File.ReadAllText("/etc/machine-id").Trim()).Append("|");
                }
            }

            // 3. عدد الأنوية والذاكرة التقريبية
            sb.Append(Environment.ProcessorCount).Append("|");
            sb.Append(Environment.SystemPageSize).Append("|");

            return sb.ToString();
        }

        private static string FormatAsMachineId(string rawInput)
        {
            using (var sha = SHA256.Create())
            {
                var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(rawInput + Salt));
                var hex = BitConverter.ToString(hashBytes).Replace("-", "").ToUpperInvariant();

                // نأخذ 12 حرف مقسمة إلى 3 مقاطع: QL-ALG-XXXX-XXXX-XXXX
                var part1 = hex.Substring(0, 4);
                var part2 = hex.Substring(4, 4);
                var part3 = hex.Substring(8, 4);

                return $"QL-ALG-{part1}-{part2}-{part3}";
            }
        }

        private static bool OperatingSystemIsWindows()
        {
            return Environment.OSVersion.Platform == PlatformID.Win32NT;
        }
    }
}
