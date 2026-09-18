using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using QLSystem.Core.Enums;
using QLSystem.Core.Interfaces;
using QLSystem.Core.Models;

namespace QLSystem.Data.Repositories
{
    /// <summary>
    /// إدارة الموردين والمشتريات (Fournisseurs & Achats)
    /// </summary>
    public class SupplierRepository : ISupplierRepository
    {
        private readonly DatabaseContext _context;

        public SupplierRepository(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Supplier>> GetAllAsync()
        {
            var list = new List<Supplier>();
            using (var conn = _context.CreateConnection())
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM Suppliers ORDER BY Name ASC;";
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            list.Add(MapSupplier(reader));
                        }
                    }
                }
            }
            return list;
        }

        public async Task<Supplier?> GetByIdAsync(int id)
        {
            using (var conn = _context.CreateConnection())
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM Suppliers WHERE Id = @id LIMIT 1;";
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return MapSupplier(reader);
                        }
                    }
                }
            }
            return null;
        }

        public async Task<int> AddAsync(Supplier s)
        {
            using (var conn = _context.CreateConnection())
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        INSERT INTO Suppliers (Name, CompanyName, Phone, Address, Wilaya, CurrentDebt, Notes, CreatedAt)
                        VALUES (@name, @company, @phone, @addr, @wilaya, @debt, @notes, @created);
                        SELECT last_insert_rowid();
                    ";
                    cmd.Parameters.AddWithValue("@name", s.Name);
                    cmd.Parameters.AddWithValue("@company", (object?)s.CompanyName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@phone", (object?)s.Phone ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@addr", (object?)s.Address ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@wilaya", (object?)s.Wilaya ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@debt", s.CurrentDebt);
                    cmd.Parameters.AddWithValue("@notes", (object?)s.Notes ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@created", s.CreatedAt.ToString("s"));

                    s.Id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    return s.Id;
                }
            }
        }

        public async Task<bool> UpdateAsync(Supplier s)
        {
            using (var conn = _context.CreateConnection())
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        UPDATE Suppliers SET
                            Name = @name, CompanyName = @company, Phone = @phone,
                            Address = @addr, Wilaya = @wilaya, CurrentDebt = @debt, Notes = @notes
                        WHERE Id = @id;
                    ";
                    cmd.Parameters.AddWithValue("@id", s.Id);
                    cmd.Parameters.AddWithValue("@name", s.Name);
                    cmd.Parameters.AddWithValue("@company", (object?)s.CompanyName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@phone", (object?)s.Phone ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@addr", (object?)s.Address ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@wilaya", (object?)s.Wilaya ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@debt", s.CurrentDebt);
                    cmd.Parameters.AddWithValue("@notes", (object?)s.Notes ?? DBNull.Value);

                    return await cmd.ExecuteNonQueryAsync() > 0;
                }
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using (var conn = _context.CreateConnection())
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM Suppliers WHERE Id = @id;";
                    cmd.Parameters.AddWithValue("@id", id);
                    return await cmd.ExecuteNonQueryAsync() > 0;
                }
            }
        }

        public async Task<int> CreatePurchaseAsync(Purchase purchase)
        {
            using (var conn = _context.CreateConnection())
            {
                await conn.OpenAsync();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = transaction;
                            cmd.CommandText = @"
                                INSERT INTO Purchases (InvoiceNumber, SupplierId, TotalAmount, PaidAmount, RemainingDebt, Notes, PurchaseDate)
                                VALUES (@inv, @supId, @total, @paid, @rem, @notes, @date);
                                SELECT last_insert_rowid();
                            ";
                            cmd.Parameters.AddWithValue("@inv", purchase.InvoiceNumber);
                            cmd.Parameters.AddWithValue("@supId", purchase.SupplierId);
                            cmd.Parameters.AddWithValue("@total", purchase.TotalAmount);
                            cmd.Parameters.AddWithValue("@paid", purchase.PaidAmount);
                            cmd.Parameters.AddWithValue("@rem", purchase.RemainingDebt);
                            cmd.Parameters.AddWithValue("@notes", (object?)purchase.Notes ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@date", purchase.PurchaseDate.ToString("s"));

                            purchase.Id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                        }

                        foreach (var item in purchase.Items)
                        {
                            using (var itemCmd = conn.CreateCommand())
                            {
                                itemCmd.Transaction = transaction;
                                itemCmd.CommandText = @"
                                    INSERT INTO PurchaseItems (PurchaseId, ProductId, ProductName, Quantity, Unit, UnitPrice, TotalPrice)
                                    VALUES (@purId, @prodId, @name, @qty, @unit, @price, @total);

                                    UPDATE Products 
                                    SET StockQuantity = StockQuantity + @qty,
                                        PurchasePrice = @price,
                                        UpdatedAt = datetime('now')
                                    WHERE Id = @prodId;
                                ";
                                itemCmd.Parameters.AddWithValue("@purId", purchase.Id);
                                itemCmd.Parameters.AddWithValue("@prodId", item.ProductId);
                                itemCmd.Parameters.AddWithValue("@name", item.ProductName);
                                itemCmd.Parameters.AddWithValue("@qty", item.Quantity);
                                itemCmd.Parameters.AddWithValue("@unit", (int)item.Unit);
                                itemCmd.Parameters.AddWithValue("@price", item.UnitPrice);
                                itemCmd.Parameters.AddWithValue("@total", item.TotalPrice);

                                await itemCmd.ExecuteNonQueryAsync();
                            }
                        }

                        if (purchase.RemainingDebt > 0)
                        {
                            using (var debtCmd = conn.CreateCommand())
                            {
                                debtCmd.Transaction = transaction;
                                debtCmd.CommandText = "UPDATE Suppliers SET CurrentDebt = CurrentDebt + @debt WHERE Id = @id;";
                                debtCmd.Parameters.AddWithValue("@debt", purchase.RemainingDebt);
                                debtCmd.Parameters.AddWithValue("@id", purchase.SupplierId);
                                await debtCmd.ExecuteNonQueryAsync();
                            }
                        }

                        transaction.Commit();
                        return purchase.Id;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public async Task<IEnumerable<Purchase>> GetRecentPurchasesAsync(int count = 50)
        {
            var list = new List<Purchase>();
            using (var conn = _context.CreateConnection())
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT p.*, s.Name as SupplierName 
                        FROM Purchases p
                        JOIN Suppliers s ON p.SupplierId = s.Id
                        ORDER BY p.Id DESC LIMIT @count;
                    ";
                    cmd.Parameters.AddWithValue("@count", count);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            list.Add(new Purchase
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                InvoiceNumber = reader["InvoiceNumber"].ToString() ?? string.Empty,
                                SupplierId = Convert.ToInt32(reader["SupplierId"]),
                                SupplierName = reader["SupplierName"].ToString(),
                                TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                                PaidAmount = Convert.ToDecimal(reader["PaidAmount"]),
                                RemainingDebt = Convert.ToDecimal(reader["RemainingDebt"]),
                                Notes = reader["Notes"] == DBNull.Value ? null : reader["Notes"].ToString(),
                                PurchaseDate = DateTime.Parse(reader["PurchaseDate"].ToString() ?? DateTime.Now.ToString())
                            });
                        }
                    }
                }
            }
            return list;
        }

        public async Task<decimal> GetTotalDebtToSuppliersAsync()
        {
            using (var conn = _context.CreateConnection())
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT COALESCE(SUM(CurrentDebt), 0) FROM Suppliers;";
                    return Convert.ToDecimal(await cmd.ExecuteScalarAsync());
                }
            }
        }

        private static Supplier MapSupplier(SqliteDataReader reader)
        {
            return new Supplier
            {
                Id = Convert.ToInt32(reader["Id"]),
                Name = reader["Name"].ToString() ?? string.Empty,
                CompanyName = reader["CompanyName"] == DBNull.Value ? null : reader["CompanyName"].ToString(),
                Phone = reader["Phone"] == DBNull.Value ? null : reader["Phone"].ToString(),
                Address = reader["Address"] == DBNull.Value ? null : reader["Address"].ToString(),
                Wilaya = reader["Wilaya"] == DBNull.Value ? null : reader["Wilaya"].ToString(),
                CurrentDebt = Convert.ToDecimal(reader["CurrentDebt"]),
                Notes = reader["Notes"] == DBNull.Value ? null : reader["Notes"].ToString(),
                CreatedAt = DateTime.Parse(reader["CreatedAt"].ToString() ?? DateTime.Now.ToString())
            };
        }
    }
}
