using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QLSystem.Core.DTOs;
using QLSystem.Core.Models;

namespace QLSystem.Core.Interfaces
{
    /// <summary>
    /// واجهة التعامل مع المبيعات والفواتير
    /// </summary>
    public interface ISaleRepository
    {
        Task<int> CreateSaleAsync(Sale sale);
        Task<Sale?> GetByIdAsync(int id);
        Task<Sale?> GetByInvoiceNumberAsync(string invoiceNumber);
        Task<IEnumerable<Sale>> GetRecentSalesAsync(int count = 50);
        Task<IEnumerable<Sale>> GetSalesByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<DailyReportDto> GetDailyReportAsync(DateTime date);
        Task<string> GenerateNextInvoiceNumberAsync();
    }
}
