using System.Collections.Generic;
using System.Threading.Tasks;
using QLSystem.Core.Models;

namespace QLSystem.Core.Interfaces
{
    /// <summary>
    /// واجهة إدارة الموردين والمشتريات
    /// </summary>
    public interface ISupplierRepository
    {
        Task<IEnumerable<Supplier>> GetAllAsync();
        Task<Supplier?> GetByIdAsync(int id);
        Task<int> AddAsync(Supplier supplier);
        Task<bool> UpdateAsync(Supplier supplier);
        Task<bool> DeleteAsync(int id);
        Task<int> CreatePurchaseAsync(Purchase purchase);
        Task<IEnumerable<Purchase>> GetRecentPurchasesAsync(int count = 50);
        Task<decimal> GetTotalDebtToSuppliersAsync();
    }
}
