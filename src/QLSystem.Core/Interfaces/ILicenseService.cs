using System.Threading.Tasks;
using QLSystem.Core.Models;

namespace QLSystem.Core.Interfaces
{
    /// <summary>
    /// واجهة نظام الترخيص، فترة الـ 7 أيام التجريبية، والتفعيل
    /// </summary>
    public interface ILicenseService
    {
        Task<LicenseInfo> CheckLicenseStatusAsync();
        Task<bool> ActivateWithKeyAsync(string activationKey);
        string GetMachineId();
    }
}
