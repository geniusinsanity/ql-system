using System.Collections.Generic;
using System.Threading.Tasks;
using QLSystem.Core.Models;

namespace QLSystem.Core.Interfaces
{
    /// <summary>
    /// واجهة التعامل مع سلع ومخزون الكانكري
    /// </summary>
    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetAllAsync();
        Task<Product?> GetByIdAsync(int id);
        Task<Product?> GetByBarcodeAsync(string barcode);
        Task<Product?> GetByReferenceAsync(string reference);

        /// <summary>
        /// بحث فوري فائق السرعة بالاسم، المرجع، المقاس، أو الباركود
        /// </summary>
        Task<IEnumerable<Product>> SearchAsync(string query, int limit = 50);

        Task<IEnumerable<Product>> GetLowStockProductsAsync();
        Task<int> AddAsync(Product product);
        Task<bool> UpdateAsync(Product product);
        Task<bool> DeleteAsync(int id);
        Task<bool> AdjustStockAsync(int productId, decimal quantityChange);
        Task<int> BulkInsertAsync(IEnumerable<Product> products);
    }
}
