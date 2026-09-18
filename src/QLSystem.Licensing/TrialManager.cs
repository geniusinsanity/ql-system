using System;
using System.IO;
using QLSystem.Core.Enums;
using QLSystem.Core.Models;

namespace QLSystem.Licensing
{
    /// <summary>
    /// مدير نظام الـ 7 أيام التجريبية والتفعيل لحاسوب المحل
    /// </summary>
    public class TrialManager
    {
        private const int TrialDaysDuration = 7;
        private readonly string _storagePath;

        public TrialManager(string? customFolder = null)
        {
            var baseFolder = customFolder ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "QLSystem", "license");

            if (!Directory.Exists(baseFolder))
            {
                Directory.CreateDirectory(baseFolder);
            }

            _storagePath = Path.Combine(baseFolder, "license.dat");
        }

        /// <summary>
        /// تقييم حالة الترخيص الحالية للجهاز
        /// </summary>
        public LicenseInfo EvaluateLicense()
        {
            var machineId = HardwareFingerprint.GetMachineId();
            var now = DateTime.Now;

            // إذا كان الملف غير موجود -> هذا أول تشغيل على الإطلاق
            if (!File.Exists(_storagePath))
            {
                var newLicense = new LicenseInfo
                {
                    MachineId = machineId,
                    FirstRunDate = now,
                    TrialEndDate = now.AddDays(TrialDaysDuration),
                    LastRunDate = now,
                    Status = LicenseStatus.TrialActive
                };

                SaveLicense(newLicense);
                return newLicense;
            }

            var license = LoadLicense(machineId);

            // 1. إذا كان البرنامج مفعل أصلاً بصفة دائمة
            if (!string.IsNullOrEmpty(license.ActivationKey) &&
                LicenseValidator.ValidateKey(machineId, license.ActivationKey))
            {
                license.Status = LicenseStatus.Activated;
                UpdateLastRunDate(license, now);
                return license;
            }

            // 2. فحص التلاعب بساعة الويندوز
            if (AntiClockTamper.IsClockManipulated(now, license.LastRunDate))
            {
                license.Status = LicenseStatus.ClockTampered;
                return license;
            }

            // 3. فحص هل انتهت الـ 7 أيام
            if (now > license.TrialEndDate)
            {
                license.Status = LicenseStatus.TrialExpired;
                UpdateLastRunDate(license, now);
                return license;
            }

            // 4. ما زال في فترة التجربة
            license.Status = LicenseStatus.TrialActive;
            UpdateLastRunDate(license, now);
            return license;
        }

        /// <summary>
        /// تفعيل البرنامج بكود الشراء
        /// </summary>
        public bool Activate(string activationKey)
        {
            var machineId = HardwareFingerprint.GetMachineId();

            if (LicenseValidator.ValidateKey(machineId, activationKey))
            {
                var license = LoadLicense(machineId);
                license.ActivationKey = activationKey.Trim().ToUpperInvariant();
                license.Status = LicenseStatus.Activated;
                license.LastRunDate = DateTime.Now;

                SaveLicense(license);
                return true;
            }

            return false;
        }

        private void UpdateLastRunDate(LicenseInfo license, DateTime now)
        {
            license.LastRunDate = now;
            SaveLicense(license);
        }

        private void SaveLicense(LicenseInfo license)
        {
            try
            {
                var line1 = license.MachineId;
                var line2 = license.ActivationKey ?? "NONE";
                var line3 = AntiClockTamper.EncryptTimestamp(license.FirstRunDate);
                var line4 = AntiClockTamper.EncryptTimestamp(license.TrialEndDate);
                var line5 = AntiClockTamper.EncryptTimestamp(license.LastRunDate);
                var line6 = ((int)license.Status).ToString();

                var content = $"{line1}\n{line2}\n{line3}\n{line4}\n{line5}\n{line6}";
                File.WriteAllText(_storagePath, content);
            }
            catch
            {
                // منع التوقف في حال وجود قيود أذونات
            }
        }

        private LicenseInfo LoadLicense(string currentMachineId)
        {
            try
            {
                var lines = File.ReadAllLines(_storagePath);
                if (lines.Length >= 6)
                {
                    return new LicenseInfo
                    {
                        MachineId = currentMachineId,
                        ActivationKey = lines[1] == "NONE" ? null : lines[1],
                        FirstRunDate = AntiClockTamper.DecryptTimestamp(lines[2]),
                        TrialEndDate = AntiClockTamper.DecryptTimestamp(lines[3]),
                        LastRunDate = AntiClockTamper.DecryptTimestamp(lines[4]),
                        Status = (LicenseStatus)int.Parse(lines[5])
                    };
                }
            }
            catch
            {
                // في حالة تلف الملف
            }

            return new LicenseInfo
            {
                MachineId = currentMachineId,
                FirstRunDate = DateTime.Now,
                TrialEndDate = DateTime.Now.AddDays(TrialDaysDuration),
                LastRunDate = DateTime.Now,
                Status = LicenseStatus.TrialActive
            };
        }
    }
}
