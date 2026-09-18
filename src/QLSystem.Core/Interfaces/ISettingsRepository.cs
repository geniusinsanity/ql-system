using System.Threading.Tasks;
using QLSystem.Core.Models;

namespace QLSystem.Core.Interfaces
{
    /// <summary>
    /// واجهة حفظ واسترجاع إعدادات المحل
    /// </summary>
    public interface ISettingsRepository
    {
        Task<StoreSettings> GetSettingsAsync();
        Task<bool> SaveSettingsAsync(StoreSettings settings);
    }
}
