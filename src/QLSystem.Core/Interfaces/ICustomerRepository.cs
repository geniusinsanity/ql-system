using System.Collections.Generic;
using System.Threading.Tasks;
using QLSystem.Core.Models;

namespace QLSystem.Core.Interfaces
{
    /// <summary>
    /// واجهة إدارة الزبائن والديون (Crédit Clients)
    /// </summary>
    public interface ICustomerRepository
    {
        Task<IEnumerable<Customer>> GetAllAsync();
        Task<Customer?> GetByIdAsync(int id);
        Task<IEnumerable<Customer>> SearchAsync(string query);
        Task<int> AddAsync(Customer customer);
        Task<bool> UpdateAsync(Customer customer);
        Task<bool> DeleteAsync(int id);
        Task<bool> RecordPaymentAsync(CustomerPayment payment);
        Task<IEnumerable<CustomerPayment>> GetPaymentHistoryAsync(int customerId);
        Task<decimal> GetTotalCreditGivenAsync();
    }
}
