using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using QLSystem.Core.DTOs;
using QLSystem.Core.Enums;
using QLSystem.Core.Interfaces;
using QLSystem.Core.Models;

namespace QLSystem.Data.Repositories
{
    /// <summary>
    /// إدارة عمليات البيع، الفواتير، والتقارير اليومية (Caisse & Ventes)
    /// </summary>
    public class SaleRepository : ISaleRepository
    {
        private readonly DatabaseContext _context;

        public SaleRepository(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateNextInvoiceNumberAsync()
        {
            using (var conn = _context.CreateConnection())
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT COUNT(*) FROM Sales;";
                    var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    var year = DateTime.Now.Year;
                    return $"TICK-{year}-{(count + 1):D5}";
                }
            }
        }

        public async Task<int> CreateSaleAsync(Sale sale)
        {
            using (var conn = _context.CreateConnection())
            {
                await conn.OpenAsync();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        if (string.IsNullOrEmpty(sale.InvoiceNumber))
                        {
                            sale.InvoiceNumber = await GenerateNextInvoiceNumberAsync();
                        }

                        // 1. تسجيل رأس الفاتورة
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = transaction;
                            cmd.CommandText = @"
                                INSERT INTO Sales (
                                    InvoiceNumber, CustomerId, CustomerName, PaymentType,
                                    SubTotal, Discount, TotalAmount, PaidAmount, ChangeAmount,
                                    DebtAmount, ProfitAmount, UserId, UserName, Notes, CreatedAt
                                ) VALUES (
                                    @inv, @custId, @custName, @payType,
                                    @sub, @disc, @total, @paid, @change,
                                    @debt, @profit, @userId, @userName, @notes, @created
                                );
                                SELECT last_insert_rowid();
                            ";
                            cmd.Parameters.AddWithValue("@inv", sale.InvoiceNumber);
                            cmd.Parameters.AddWithValue("@custId", (object?)sale.CustomerId ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@custName", sale.CustomerName);
                            cmd.Parameters.AddWithValue("@payType", (int)sale.PaymentType);
                            cmd.Parameters.AddWithValue("@sub", sale.SubTotal);
                            cmd.Parameters.AddWithValue("@disc", sale.Discount);
                            cmd.Parameters.AddWithValue("@total", sale.TotalAmount);
                            cmd.Parameters.AddWithValue("@paid", sale.PaidAmount);
                            cmd.Parameters.AddWithValue("@change", sale.ChangeAmount);
                            cmd.Parameters.AddWithValue("@debt", sale.DebtAmount);
                            cmd.Parameters.AddWithValue("@profit", sale.ProfitAmount);
                            cmd.Parameters.AddWithValue("@userId", sale.UserId);
                            cmd.Parameters.AddWithValue("@userName", (object?)sale.UserName ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@notes", (object?)sale.Notes ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@created", sale.CreatedAt.ToString("s"));

                            sale.Id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                        }

                        // 2. تسجيل عناصر الفاتورة وإنقاص المخزن
                        foreach (var item in sale.Items)
                        {
                            using (var itemCmd = conn.CreateCommand())
                            {
                                itemCmd.Transaction = transaction;
                                itemCmd.CommandText = @"
                                    INSERT INTO SaleItems (
                                        SaleId, ProductId, ProductReference, ProductName,
                                        Quantity, Unit, UnitPrice, PurchasePrice, TotalPrice
                                    ) VALUES (
                                        @saleId, @prodId, @ref, @name,
                                        @qty, @unit, @price, @cost, @total
                                    );

                                    UPDATE Products 
                                    SET StockQuantity = StockQuantity - @qty,
                                        UpdatedAt = datetime('now')
                                    WHERE Id = @prodId;
                                ";
                                itemCmd.Parameters.AddWithValue("@saleId", sale.Id);
                                itemCmd.Parameters.AddWithValue("@prodId", item.ProductId);
                                itemCmd.Parameters.AddWithValue("@ref", item.ProductReference);
                                itemCmd.Parameters.AddWithValue("@name", item.ProductName);
                                itemCmd.Parameters.AddWithValue("@qty", item.Quantity);
                                itemCmd.Parameters.AddWithValue("@unit", (int)item.Unit);
                                itemCmd.Parameters.AddWithValue("@price", item.UnitPrice);
                                itemCmd.Parameters.AddWithValue("@cost", item.PurchasePrice);
                                itemCmd.Parameters.AddWithValue("@total", item.TotalPrice);

                                await itemCmd.ExecuteNonQueryAsync();
                            }
                        }

                        // 3. إذا كان البيع بالكريدي وفيه دين، نزيد في رصيد دين الزبون
                        if (sale.CustomerId.HasValue && sale.DebtAmount > 0)
                        {
                            using (var debtCmd = conn.CreateCommand())
                            {
                                debtCmd.Transaction = transaction;
                                debtCmd.CommandText = @"
                                    UPDATE Customers 
                                    SET CurrentDebt = CurrentDebt + @debt,
                                        UpdatedAt = datetime('now')
                                    WHERE Id = @custId;
                                ";
                                debtCmd.Parameters.AddWithValue("@debt", sale.DebtAmount);
                                debtCmd.Parameters.AddWithValue("@custId", sale.CustomerId.Value);
                                await debtCmd.ExecuteNonQueryAsync();
                            }
                        }

                        transaction.Commit();
                        return sale.Id;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public async Task<Sale?> GetByIdAsync(int id)
        {
            using (var conn = _context.CreateConnection())
            {
                await conn.OpenAsync();
                Sale? sale = null;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM Sales WHERE Id = @id LIMIT 1;";
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            sale = MapSale(reader);
                        }
                    }
                }

                if (sale != null)
                {
                    sale.Items = await GetSaleItemsAsync(conn, sale.Id);
                }

                return sale;
            }
        }

        public async Task<Sale?> GetByInvoiceNumberAsync(string invoiceNumber)
        {
            using (var conn = _context.CreateConnection())
            {
                await conn.OpenAsync();
                Sale? sale = null;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM Sales WHERE InvoiceNumber = @inv LIMIT 1;";
                    cmd.Parameters.AddWithValue("@inv", invoiceNumber);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            sale = MapSale(reader);
                        }
                    }
                }

                if (sale != null)
                {
                    sale.Items = await GetSaleItemsAsync(conn, sale.Id);
                }

                return sale;
            }
        }

        public async Task<IEnumerable<Sale>> GetRecentSalesAsync(int count = 50)
        {
            var list = new List<Sale>();
            using (var conn = _context.CreateConnection())
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM Sales ORDER BY Id DESC LIMIT @count;";
                    cmd.Parameters.AddWithValue("@count", count);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            list.Add(MapSale(reader));
                        }
                    }
                }
            }
            return list;
        }

        public async Task<IEnumerable<Sale>> GetSalesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            var list = new List<Sale>();
            using (var conn = _context.CreateConnection())
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT * FROM Sales 
                        WHERE CreatedAt >= @start AND CreatedAt <= @end 
                        ORDER BY Id DESC;
                    ";
                    cmd.Parameters.AddWithValue("@start", startDate.Date.ToString("s"));
                    cmd.Parameters.AddWithValue("@end", endDate.Date.AddDays(1).AddTicks(-1).ToString("s"));
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            list.Add(MapSale(reader));
                        }
                    }
                }
            }
            return list;
        }

        public async Task<DailyReportDto> GetDailyReportAsync(DateTime date)
        {
            var start = date.Date.ToString("s");
            var end = date.Date.AddDays(1).AddTicks(-1).ToString("s");

            var report = new DailyReportDto { Date = date.Date };

            using (var conn = _context.CreateConnection())
            {
                await conn.OpenAsync();

                // 1. حساب مبيعات اليوم
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT 
                            COUNT(*) as TotalCount,
                            COALESCE(SUM(TotalAmount), 0) as TotalSales,
                            COALESCE(SUM(PaidAmount), 0) as CashCollected,
                            COALESCE(SUM(DebtAmount), 0) as CreditGiven,
                            COALESCE(SUM(ProfitAmount), 0) as NetProfit
                        FROM Sales 
                        WHERE CreatedAt >= @start AND CreatedAt <= @end;
                    ";
                    cmd.Parameters.AddWithValue("@start", start);
                    cmd.Parameters.AddWithValue("@end", end);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            report.TotalTransactions = Convert.ToInt32(reader["TotalCount"]);
                            report.TotalSales = Convert.ToDecimal(reader["TotalSales"]);
                            report.CashCollected = Convert.ToDecimal(reader["CashCollected"]);
                            report.CreditGiven = Convert.ToDecimal(reader["CreditGiven"]);
                            report.NetProfit = Convert.ToDecimal(reader["NetProfit"]);
                        }
                    }
                }

                // 2. حساب دفعات الكريدي المستلمة اليوم
                using (var pCmd = conn.CreateCommand())
                {
                    pCmd.CommandText = @"
                        SELECT COALESCE(SUM(Amount), 0) 
                        FROM CustomerPayments 
                        WHERE PaymentDate >= @start AND PaymentDate <= @end;
                    ";
                    pCmd.Parameters.AddWithValue("@start", start);
                    pCmd.Parameters.AddWithValue("@end", end);
                    report.DebtsCollected = Convert.ToDecimal(await pCmd.ExecuteScalarAsync());
                }

                // 3. عدد تنبيهات نفاد المخزون
                using (var aCmd = conn.CreateCommand())
                {
                    aCmd.CommandText = "SELECT COUNT(*) FROM Products WHERE IsActive = 1 AND StockQuantity <= MinStockAlert;";
                    report.LowStockAlertsCount = Convert.ToInt32(await aCmd.ExecuteScalarAsync());
                }
            }

            return report;
        }

        private async Task<List<SaleItem>> GetSaleItemsAsync(SqliteConnection conn, int saleId)
        {
            var items = new List<SaleItem>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM SaleItems WHERE SaleId = @saleId;";
                cmd.Parameters.AddWithValue("@saleId", saleId);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        items.Add(new SaleItem
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            SaleId = Convert.ToInt32(reader["SaleId"]),
                            ProductId = Convert.ToInt32(reader["ProductId"]),
                            ProductReference = reader["ProductReference"].ToString() ?? string.Empty,
                            ProductName = reader["ProductName"].ToString() ?? string.Empty,
                            Quantity = Convert.ToDecimal(reader["Quantity"]),
                            Unit = (UnitType)Convert.ToInt32(reader["Unit"]),
                            UnitPrice = Convert.ToDecimal(reader["UnitPrice"]),
                            PurchasePrice = Convert.ToDecimal(reader["PurchasePrice"]),
                            TotalPrice = Convert.ToDecimal(reader["TotalPrice"])
                        });
                    }
                }
            }
            return items;
        }

        private static Sale MapSale(SqliteDataReader reader)
        {
            return new Sale
            {
                Id = Convert.ToInt32(reader["Id"]),
                InvoiceNumber = reader["InvoiceNumber"].ToString() ?? string.Empty,
                CustomerId = reader["CustomerId"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["CustomerId"]),
                CustomerName = reader["CustomerName"].ToString() ?? "Comptoir",
                PaymentType = (PaymentType)Convert.ToInt32(reader["PaymentType"]),
                SubTotal = Convert.ToDecimal(reader["SubTotal"]),
                Discount = Convert.ToDecimal(reader["Discount"]),
                TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                PaidAmount = Convert.ToDecimal(reader["PaidAmount"]),
                ChangeAmount = Convert.ToDecimal(reader["ChangeAmount"]),
                DebtAmount = Convert.ToDecimal(reader["DebtAmount"]),
                ProfitAmount = Convert.ToDecimal(reader["ProfitAmount"]),
                UserId = Convert.ToInt32(reader["UserId"]),
                UserName = reader["UserName"] == DBNull.Value ? null : reader["UserName"].ToString(),
                Notes = reader["Notes"] == DBNull.Value ? null : reader["Notes"].ToString(),
                CreatedAt = DateTime.Parse(reader["CreatedAt"].ToString() ?? DateTime.Now.ToString())
            };
        }
    }
}
