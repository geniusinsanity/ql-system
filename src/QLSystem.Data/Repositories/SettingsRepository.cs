using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using QLSystem.Core.Interfaces;
using QLSystem.Core.Models;

namespace QLSystem.Data.Repositories
{
    /// <summary>
    /// إدارة إعدادات المحل والطباعة والنسخ الاحتياطي
    /// </summary>
    public class SettingsRepository : ISettingsRepository
    {
        private readonly DatabaseContext _context;

        public SettingsRepository(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<StoreSettings> GetSettingsAsync()
        {
            using (var conn = _context.CreateConnection())
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM StoreSettings WHERE Id = 1 LIMIT 1;";
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new StoreSettings
                            {
                                Id = 1,
                                StoreName = reader["StoreName"].ToString() ?? "Quincaillerie",
                                OwnerName = reader["OwnerName"] == DBNull.Value ? null : reader["OwnerName"].ToString(),
                                Phone1 = reader["Phone1"] == DBNull.Value ? null : reader["Phone1"].ToString(),
                                Phone2 = reader["Phone2"] == DBNull.Value ? null : reader["Phone2"].ToString(),
                                Address = reader["Address"] == DBNull.Value ? null : reader["Address"].ToString(),
                                ReceiptHeader = reader["ReceiptHeader"] == DBNull.Value ? "Bienvenue" : reader["ReceiptHeader"].ToString() ?? "Bienvenue",
                                ReceiptFooter = reader["ReceiptFooter"] == DBNull.Value ? "Merci" : reader["ReceiptFooter"].ToString() ?? "Merci",
                                ThermalPrinterName = reader["ThermalPrinterName"] == DBNull.Value ? null : reader["ThermalPrinterName"].ToString(),
                                ThermalPrinterWidthMm = Convert.ToInt32(reader["ThermalPrinterWidthMm"]),
                                AutoPrintReceipt = Convert.ToInt32(reader["AutoPrintReceipt"]) == 1,
                                AutoOpenCashDrawer = Convert.ToInt32(reader["AutoOpenCashDrawer"]) == 1,
                                BackupFolder = reader["BackupFolder"] == DBNull.Value ? string.Empty : reader["BackupFolder"].ToString() ?? string.Empty,
                                Language = reader["Language"] == DBNull.Value ? "fr" : reader["Language"].ToString() ?? "fr"
                            };
                        }
                    }
                }
            }

            return new StoreSettings();
        }

        public async Task<bool> SaveSettingsAsync(StoreSettings s)
        {
            using (var conn = _context.CreateConnection())
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        INSERT OR REPLACE INTO StoreSettings (
                            Id, StoreName, OwnerName, Phone1, Phone2, Address,
                            ReceiptHeader, ReceiptFooter, ThermalPrinterName,
                            ThermalPrinterWidthMm, AutoPrintReceipt, AutoOpenCashDrawer,
                            BackupFolder, Language
                        ) VALUES (
                            1, @name, @owner, @p1, @p2, @addr,
                            @head, @foot, @printer,
                            @width, @autoPrint, @autoDrawer,
                            @backup, @lang
                        );
                    ";
                    cmd.Parameters.AddWithValue("@name", s.StoreName);
                    cmd.Parameters.AddWithValue("@owner", (object?)s.OwnerName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@p1", (object?)s.Phone1 ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@p2", (object?)s.Phone2 ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@addr", (object?)s.Address ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@head", (object?)s.ReceiptHeader ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@foot", (object?)s.ReceiptFooter ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@printer", (object?)s.ThermalPrinterName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@width", s.ThermalPrinterWidthMm);
                    cmd.Parameters.AddWithValue("@autoPrint", s.AutoPrintReceipt ? 1 : 0);
                    cmd.Parameters.AddWithValue("@autoDrawer", s.AutoOpenCashDrawer ? 1 : 0);
                    cmd.Parameters.AddWithValue("@backup", (object?)s.BackupFolder ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@lang", s.Language);

                    return await cmd.ExecuteNonQueryAsync() > 0;
                }
            }
        }
    }
}
