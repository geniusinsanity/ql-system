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
    /// إدارة ديون الزبائن والتسديدات (Crédit Clients & Versements)
    /// </summary>
    public class CustomerRepository : ICustomerRepository
    {
        private readonly DatabaseContext _context;

        public CustomerRepository(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Customer>> GetAllAsync()
        {
            var list = new List<Customer>();
            using (var conn = _context.CreateConnection())
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM Customers ORDER BY CurrentDebt DESC, FullName ASC;";
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            list.Add(MapCustomer(reader));
                        }
                    }
                }
            }
            return list;
        }

        public async Task<Customer?> GetByIdAsync(int id)
        {
            using (var conn = _context.CreateConnection())
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM Customers WHERE Id = @id LIMIT 1;";
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return MapCustomer(reader);
                        }
                    }
                }
            }
            return null;
        }

        public async Task<IEnumerable<Customer>> SearchAsync(string query)
        {
            var list = new List<Customer>();
            if (string.IsNullOrWhiteSpace(query)) return await GetAllAsync();

            using (var conn = _context.CreateConnection())
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT * FROM Customers 
                        WHERE FullName LIKE @q OR Phone LIKE @q OR Activity LIKE @q
                        ORDER BY CurrentDebt DESC;
                    ";
                    cmd.Parameters.AddWithValue("@q", "%" + query.Trim() + "%");
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            list.Add(MapCustomer(reader));
                        }
                    }
                }
            }
            return list;
        }

        public async Task<int> AddAsync(Customer customer)
        {
            using (var conn = _context.CreateConnection())
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        INSERT INTO Customers (
                            FullName, Phone, Address, Activity, CurrentDebt,
                            MaxCreditLimit, AppliesWholesalePrice, Notes, CreatedAt, UpdatedAt
                        ) VALUES (
                            @name, @phone, @addr, @act, @debt,
                            @limit, @wholesale, @notes, @created, @updated
                        );
                        SELECT last_insert_rowid();
                    ";
                    cmd.Parameters.AddWithValue("@name", customer.FullName);
                    cmd.Parameters.AddWithValue("@phone", (object?)customer.Phone ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@addr", (object?)customer.Address ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@act", (object?)customer.Activity ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@debt", customer.CurrentDebt);
                    cmd.Parameters.AddWithValue("@limit", customer.MaxCreditLimit);
                    cmd.Parameters.AddWithValue("@wholesale", customer.AppliesWholesalePrice ? 1 : 0);
                    cmd.Parameters.AddWithValue("@notes", (object?)customer.Notes ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@created", customer.CreatedAt.ToString("s"));
                    cmd.Parameters.AddWithValue("@updated", customer.UpdatedAt.ToString("s"));

                    customer.Id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    return customer.Id;
                }
            }
        }

        public async Task<bool> UpdateAsync(Customer customer)
        {
            using (var conn = _context.CreateConnection())
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        UPDATE Customers SET
                            FullName = @name,
                            Phone = @phone,
                            Address = @addr,
                            Activity = @act,
                            CurrentDebt = @debt,
                            MaxCreditLimit = @limit,
                            AppliesWholesalePrice = @wholesale,
                            Notes = @notes,
                            UpdatedAt = @updated
                        WHERE Id = @id;
                    ";
                    cmd.Parameters.AddWithValue("@id", customer.Id);
                    cmd.Parameters.AddWithValue("@name", customer.FullName);
                    cmd.Parameters.AddWithValue("@phone", (object?)customer.Phone ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@addr", (object?)customer.Address ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@act", (object?)customer.Activity ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@debt", customer.CurrentDebt);
                    cmd.Parameters.AddWithValue("@limit", customer.MaxCreditLimit);
                    cmd.Parameters.AddWithValue("@wholesale", customer.AppliesWholesalePrice ? 1 : 0);
                    cmd.Parameters.AddWithValue("@notes", (object?)customer.Notes ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@updated", DateTime.Now.ToString("s"));

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
                    cmd.CommandText = "DELETE FROM Customers WHERE Id = @id;";
                    cmd.Parameters.AddWithValue("@id", id);
                    return await cmd.ExecuteNonQueryAsync() > 0;
                }
            }
        }

        public async Task<bool> RecordPaymentAsync(CustomerPayment payment)
        {
            using (var conn = _context.CreateConnection())
            {
                await conn.OpenAsync();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. جلب الرصيد الحالي للزبون
                        decimal currentDebt = 0;
                        using (var getCmd = conn.CreateCommand())
                        {
                            getCmd.Transaction = transaction;
                            getCmd.CommandText = "SELECT CurrentDebt FROM Customers WHERE Id = @id;";
                            getCmd.Parameters.AddWithValue("@id", payment.CustomerId);
                            var result = await getCmd.ExecuteScalarAsync();
                            if (result == null) return false;
                            currentDebt = Convert.ToDecimal(result);
                        }

                        payment.PreviousDebt = currentDebt;
                        payment.RemainingDebt = currentDebt - payment.Amount;
                        if (payment.RemainingDebt < 0) payment.RemainingDebt = 0;

                        // 2. إدخال سجل الدفعة
                        using (var payCmd = conn.CreateCommand())
                        {
                            payCmd.Transaction = transaction;
                            payCmd.CommandText = @"
                                INSERT INTO CustomerPayments (
                                    CustomerId, Amount, PreviousDebt, RemainingDebt,
                                    PaymentMethod, ReferenceNumber, Notes, CreatedByUserId, PaymentDate
                                ) VALUES (
                                    @custId, @amount, @prev, @rem,
                                    @method, @ref, @notes, @userId, @date
                                );
                            ";
                            payCmd.Parameters.AddWithValue("@custId", payment.CustomerId);
                            payCmd.Parameters.AddWithValue("@amount", payment.Amount);
                            payCmd.Parameters.AddWithValue("@prev", payment.PreviousDebt);
                            payCmd.Parameters.AddWithValue("@rem", payment.RemainingDebt);
                            payCmd.Parameters.AddWithValue("@method", (int)payment.PaymentMethod);
                            payCmd.Parameters.AddWithValue("@ref", (object?)payment.ReferenceNumber ?? DBNull.Value);
                            payCmd.Parameters.AddWithValue("@notes", (object?)payment.Notes ?? DBNull.Value);
                            payCmd.Parameters.AddWithValue("@userId", payment.CreatedByUserId);
                            payCmd.Parameters.AddWithValue("@date", payment.PaymentDate.ToString("s"));

                            await payCmd.ExecuteNonQueryAsync();
                        }

                        // 3. تحديث دين الزبون
                        using (var updateCmd = conn.CreateCommand())
                        {
                            updateCmd.Transaction = transaction;
                            updateCmd.CommandText = @"
                                UPDATE Customers 
                                SET CurrentDebt = @newDebt,
                                    UpdatedAt = datetime('now')
                                WHERE Id = @custId;
                            ";
                            updateCmd.Parameters.AddWithValue("@newDebt", payment.RemainingDebt);
                            updateCmd.Parameters.AddWithValue("@custId", payment.CustomerId);
                            await updateCmd.ExecuteNonQueryAsync();
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public async Task<IEnumerable<CustomerPayment>> GetPaymentHistoryAsync(int customerId)
        {
            var list = new List<CustomerPayment>();
            using (var conn = _context.CreateConnection())
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT cp.*, c.FullName as CustomerName 
                        FROM CustomerPayments cp
                        JOIN Customers c ON cp.CustomerId = c.Id
                        WHERE cp.CustomerId = @custId
                        ORDER BY cp.PaymentDate DESC;
                    ";
                    cmd.Parameters.AddWithValue("@custId", customerId);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            list.Add(new CustomerPayment
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                CustomerId = Convert.ToInt32(reader["CustomerId"]),
                                CustomerName = reader["CustomerName"].ToString(),
                                Amount = Convert.ToDecimal(reader["Amount"]),
                                PreviousDebt = Convert.ToDecimal(reader["PreviousDebt"]),
                                RemainingDebt = Convert.ToDecimal(reader["RemainingDebt"]),
                                PaymentMethod = (PaymentType)Convert.ToInt32(reader["PaymentMethod"]),
                                ReferenceNumber = reader["ReferenceNumber"] == DBNull.Value ? null : reader["ReferenceNumber"].ToString(),
                                Notes = reader["Notes"] == DBNull.Value ? null : reader["Notes"].ToString(),
                                CreatedByUserId = Convert.ToInt32(reader["CreatedByUserId"]),
                                PaymentDate = DateTime.Parse(reader["PaymentDate"].ToString() ?? DateTime.Now.ToString())
                            });
                        }
                    }
                }
            }
            return list;
        }

        public async Task<decimal> GetTotalCreditGivenAsync()
        {
            using (var conn = _context.CreateConnection())
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT COALESCE(SUM(CurrentDebt), 0) FROM Customers;";
                    return Convert.ToDecimal(await cmd.ExecuteScalarAsync());
                }
            }
        }

        private static Customer MapCustomer(SqliteDataReader reader)
        {
            return new Customer
            {
                Id = Convert.ToInt32(reader["Id"]),
                FullName = reader["FullName"].ToString() ?? string.Empty,
                Phone = reader["Phone"] == DBNull.Value ? null : reader["Phone"].ToString(),
                Address = reader["Address"] == DBNull.Value ? null : reader["Address"].ToString(),
                Activity = reader["Activity"] == DBNull.Value ? null : reader["Activity"].ToString(),
                CurrentDebt = Convert.ToDecimal(reader["CurrentDebt"]),
                MaxCreditLimit = Convert.ToDecimal(reader["MaxCreditLimit"]),
                AppliesWholesalePrice = Convert.ToInt32(reader["AppliesWholesalePrice"]) == 1,
                Notes = reader["Notes"] == DBNull.Value ? null : reader["Notes"].ToString(),
                CreatedAt = DateTime.Parse(reader["CreatedAt"].ToString() ?? DateTime.Now.ToString()),
                UpdatedAt = DateTime.Parse(reader["UpdatedAt"].ToString() ?? DateTime.Now.ToString())
            };
        }
    }
}
