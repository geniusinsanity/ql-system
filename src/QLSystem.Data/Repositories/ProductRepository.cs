using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using QLSystem.Core.Enums;
using QLSystem.Core.Interfaces;
using QLSystem.Core.Models;

namespace QLSystem.Data.Repositories
{
    /// <summary>
    /// إدارة عمليات قاعدة البيانات لسلع الكانكري (Recherche instantanée & Stock)
    /// </summary>
    public class ProductRepository : IProductRepository
    {
        private readonly DatabaseContext _context;

        public ProductRepository(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            var list = new List<Product>();
            using (var conn = _context.CreateConnection())
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT p.*, c.Name as CategoryName 
                        FROM Products p 
                        LEFT JOIN Categories c ON p.CategoryId = c.Id
                        WHERE p.IsActive = 1
                        ORDER BY p.Name ASC;
                    ";
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            list.Add(MapProduct(reader));
                        }
                    }
                }
            }
            return list;
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            using (var conn = _context.CreateConnection())
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT p.*, c.Name as CategoryName 
                        FROM Products p 
                        LEFT JOIN Categories c ON p.CategoryId = c.Id
                        WHERE p.Id = @id LIMIT 1;
                    ";
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return MapProduct(reader);
                        }
                    }
                }
            }
            return null;
        }

        public async Task<Product?> GetByBarcodeAsync(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode)) return null;

            using (var conn = _context.CreateConnection())
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT p.*, c.Name as CategoryName 
                        FROM Products p 
                        LEFT JOIN Categories c ON p.CategoryId = c.Id
                        WHERE p.Barcode = @barcode AND p.IsActive = 1 LIMIT 1;
                    ";
                    cmd.Parameters.AddWithValue("@barcode", barcode.Trim());
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return MapProduct(reader);
                        }
                    }
                }
            }
            return null;
        }

        public async Task<Product?> GetByReferenceAsync(string reference)
        {
            if (string.IsNullOrWhiteSpace(reference)) return null;

            using (var conn = _context.CreateConnection())
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT p.*, c.Name as CategoryName 
                        FROM Products p 
                        LEFT JOIN Categories c ON p.CategoryId = c.Id
                        WHERE UPPER(p.Reference) = UPPER(@ref) AND p.IsActive = 1 LIMIT 1;
                    ";
                    cmd.Parameters.AddWithValue("@ref", reference.Trim());
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return MapProduct(reader);
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// بحث فوري فائق السرعة للكانكري بالاسم أو الباركود أو المرجع أو المقاسات
        /// </summary>
        public async Task<IEnumerable<Product>> SearchAsync(string query, int limit = 50)
        {
            var list = new List<Product>();
            if (string.IsNullOrWhiteSpace(query)) return await GetAllAsync();

            var cleanQuery = query.Trim();

            using (var conn = _context.CreateConnection())
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    // فحص الباركود الدقيق أولاً، ثم البحث المرن
                    cmd.CommandText = @"
                        SELECT p.*, c.Name as CategoryName 
                        FROM Products p 
                        LEFT JOIN Categories c ON p.CategoryId = c.Id
                        WHERE p.IsActive = 1 AND (
                            p.Barcode = @exactQuery OR
                            p.Reference LIKE @likeQuery OR
                            p.Name LIKE @likeQuery OR
                            p.NameAr LIKE @likeQuery OR
                            p.Dimensions LIKE @likeQuery
                        )
                        ORDER BY 
                            CASE WHEN p.Barcode = @exactQuery THEN 1
                                 WHEN p.Reference LIKE @startQuery THEN 2
                                 WHEN p.Name LIKE @startQuery THEN 3
                                 ELSE 4 END,
                            p.Name ASC
                        LIMIT @limit;
                    ";
                    cmd.Parameters.AddWithValue("@exactQuery", cleanQuery);
                    cmd.Parameters.AddWithValue("@startQuery", cleanQuery + "%");
                    cmd.Parameters.AddWithValue("@likeQuery", "%" + cleanQuery + "%");
                    cmd.Parameters.AddWithValue("@limit", limit);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            list.Add(MapProduct(reader));
                        }
                    }
                }
            }
            return list;
        }

        public async Task<IEnumerable<Product>> GetLowStockProductsAsync()
        {
            var list = new List<Product>();
            using (var conn = _context.CreateConnection())
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT p.*, c.Name as CategoryName 
                        FROM Products p 
                        LEFT JOIN Categories c ON p.CategoryId = c.Id
                        WHERE p.IsActive = 1 AND p.StockQuantity <= p.MinStockAlert
                        ORDER BY p.StockQuantity ASC;
                    ";
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            list.Add(MapProduct(reader));
                        }
                    }
                }
            }
            return list;
        }

        public async Task<int> AddAsync(Product p)
        {
            using (var conn = _context.CreateConnection())
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        INSERT INTO Products (
                            Reference, Barcode, Name, NameAr, CategoryId, Dimensions, Brand,
                            PurchasePrice, SalePrice, WholesalePrice, StockQuantity, MinStockAlert,
                            SaleUnit, PurchaseUnit, ConversionFactor, Location, DefaultSupplierId,
                            IsActive, CreatedAt, UpdatedAt
                        ) VALUES (
                            @ref, @barcode, @name, @nameAr, @catId, @dim, @brand,
                            @cost, @price, @wholesale, @stock, @minStock,
                            @sUnit, @pUnit, @factor, @loc, @supId,
                            @active, @created, @updated
                        );
                        SELECT last_insert_rowid();
                    ";
                    AddProductParameters(cmd, p);
                    var newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    p.Id = newId;
                    return newId;
                }
            }
        }

        public async Task<bool> UpdateAsync(Product p)
        {
            using (var conn = _context.CreateConnection())
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        UPDATE Products SET
                            Reference = @ref,
                            Barcode = @barcode,
                            Name = @name,
                            NameAr = @nameAr,
                            CategoryId = @catId,
                            Dimensions = @dim,
                            Brand = @brand,
                            PurchasePrice = @cost,
                            SalePrice = @price,
                            WholesalePrice = @wholesale,
                            StockQuantity = @stock,
                            MinStockAlert = @minStock,
                            SaleUnit = @sUnit,
                            PurchaseUnit = @pUnit,
                            ConversionFactor = @factor,
                            Location = @loc,
                            DefaultSupplierId = @supId,
                            IsActive = @active,
                            UpdatedAt = @updated
                        WHERE Id = @id;
                    ";
                    AddProductParameters(cmd, p);
                    cmd.Parameters.AddWithValue("@id", p.Id);
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
                    // Soft delete لعدم كسر الفواتير التاريخية
                    cmd.CommandText = "UPDATE Products SET IsActive = 0 WHERE Id = @id;";
                    cmd.Parameters.AddWithValue("@id", id);
                    return await cmd.ExecuteNonQueryAsync() > 0;
                }
            }
        }

        public async Task<bool> AdjustStockAsync(int productId, decimal quantityChange)
        {
            using (var conn = _context.CreateConnection())
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        UPDATE Products 
                        SET StockQuantity = StockQuantity + @qty,
                            UpdatedAt = @updated
                        WHERE Id = @id;
                    ";
                    cmd.Parameters.AddWithValue("@qty", quantityChange);
                    cmd.Parameters.AddWithValue("@updated", DateTime.Now.ToString("s"));
                    cmd.Parameters.AddWithValue("@id", productId);
                    return await cmd.ExecuteNonQueryAsync() > 0;
                }
            }
        }

        public async Task<int> BulkInsertAsync(IEnumerable<Product> products)
        {
            int inserted = 0;
            using (var conn = _context.CreateConnection())
            {
                await conn.OpenAsync();
                using (var transaction = conn.BeginTransaction())
                {
                    foreach (var p in products)
                    {
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = transaction;
                            cmd.CommandText = @"
                                INSERT OR REPLACE INTO Products (
                                    Reference, Barcode, Name, NameAr, CategoryId, Dimensions, Brand,
                                    PurchasePrice, SalePrice, WholesalePrice, StockQuantity, MinStockAlert,
                                    SaleUnit, PurchaseUnit, ConversionFactor, Location, DefaultSupplierId,
                                    IsActive, CreatedAt, UpdatedAt
                                ) VALUES (
                                    @ref, @barcode, @name, @nameAr, @catId, @dim, @brand,
                                    @cost, @price, @wholesale, @stock, @minStock,
                                    @sUnit, @pUnit, @factor, @loc, @supId,
                                    @active, @created, @updated
                                );
                            ";
                            AddProductParameters(cmd, p);
                            await cmd.ExecuteNonQueryAsync();
                            inserted++;
                        }
                    }
                    transaction.Commit();
                }
            }
            return inserted;
        }

        private static void AddProductParameters(SqliteCommand cmd, Product p)
        {
            cmd.Parameters.AddWithValue("@ref", p.Reference);
            cmd.Parameters.AddWithValue("@barcode", (object?)p.Barcode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@name", p.Name);
            cmd.Parameters.AddWithValue("@nameAr", (object?)p.NameAr ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@catId", p.CategoryId);
            cmd.Parameters.AddWithValue("@dim", (object?)p.Dimensions ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@brand", (object?)p.Brand ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cost", p.PurchasePrice);
            cmd.Parameters.AddWithValue("@price", p.SalePrice);
            cmd.Parameters.AddWithValue("@wholesale", p.WholesalePrice);
            cmd.Parameters.AddWithValue("@stock", p.StockQuantity);
            cmd.Parameters.AddWithValue("@minStock", p.MinStockAlert);
            cmd.Parameters.AddWithValue("@sUnit", (int)p.SaleUnit);
            cmd.Parameters.AddWithValue("@pUnit", (int)p.PurchaseUnit);
            cmd.Parameters.AddWithValue("@factor", p.ConversionFactor);
            cmd.Parameters.AddWithValue("@loc", (object?)p.Location ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@supId", (object?)p.DefaultSupplierId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@active", p.IsActive ? 1 : 0);
            cmd.Parameters.AddWithValue("@created", p.CreatedAt.ToString("s"));
            cmd.Parameters.AddWithValue("@updated", DateTime.Now.ToString("s"));
        }

        private static Product MapProduct(DbDataReader reader)
        {
            return new Product
            {
                Id = Convert.ToInt32(reader["Id"]),
                Reference = reader["Reference"].ToString() ?? string.Empty,
                Barcode = reader["Barcode"] == DBNull.Value ? null : reader["Barcode"].ToString(),
                Name = reader["Name"].ToString() ?? string.Empty,
                NameAr = reader["NameAr"] == DBNull.Value ? null : reader["NameAr"].ToString(),
                CategoryId = Convert.ToInt32(reader["CategoryId"]),
                CategoryName = reader["CategoryName"] == DBNull.Value ? null : reader["CategoryName"].ToString(),
                Dimensions = reader["Dimensions"] == DBNull.Value ? null : reader["Dimensions"].ToString(),
                Brand = reader["Brand"] == DBNull.Value ? null : reader["Brand"].ToString(),
                PurchasePrice = Convert.ToDecimal(reader["PurchasePrice"]),
                SalePrice = Convert.ToDecimal(reader["SalePrice"]),
                WholesalePrice = Convert.ToDecimal(reader["WholesalePrice"]),
                StockQuantity = Convert.ToDecimal(reader["StockQuantity"]),
                MinStockAlert = Convert.ToDecimal(reader["MinStockAlert"]),
                SaleUnit = (UnitType)Convert.ToInt32(reader["SaleUnit"]),
                PurchaseUnit = (UnitType)Convert.ToInt32(reader["PurchaseUnit"]),
                ConversionFactor = Convert.ToDecimal(reader["ConversionFactor"]),
                Location = reader["Location"] == DBNull.Value ? null : reader["Location"].ToString(),
                DefaultSupplierId = reader["DefaultSupplierId"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["DefaultSupplierId"]),
                IsActive = Convert.ToInt32(reader["IsActive"]) == 1,
                CreatedAt = DateTime.Parse(reader["CreatedAt"].ToString() ?? DateTime.Now.ToString()),
                UpdatedAt = DateTime.Parse(reader["UpdatedAt"].ToString() ?? DateTime.Now.ToString())
            };
        }
    }
}
