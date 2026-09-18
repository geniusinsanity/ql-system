using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace QLSystem.Data
{
    /// <summary>
    /// إدارة اتصال وقاعدة بيانات SQLite المحلية لبرنامج الكانكري
    /// </summary>
    public class DatabaseContext
    {
        private readonly string _connectionString;
        public string DatabasePath { get; }

        public DatabaseContext(string? customDbPath = null)
        {
            if (string.IsNullOrEmpty(customDbPath))
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var folder = Path.Combine(appData, "QLSystem", "data");
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
                DatabasePath = Path.Combine(folder, "quincaillerie.db");
            }
            else
            {
                DatabasePath = customDbPath;
                var folder = Path.GetDirectoryName(DatabasePath);
                if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
            }

            _connectionString = $"Data Source={DatabasePath}";
        }

        public SqliteConnection CreateConnection()
        {
            return new SqliteConnection(_connectionString);
        }

        /// <summary>
        /// تهيئة الجداول والفهارس في قاعدة البيانات
        /// </summary>
        public async Task InitializeAsync()
        {
            using (var connection = CreateConnection())
            {
                await connection.OpenAsync();

                // تفعيل وضع WAL فائق السرعة لـ SQLite
                using (var pragmaCmd = connection.CreateCommand())
                {
                    pragmaCmd.CommandText = @"
                        PRAGMA journal_mode = WAL;
                        PRAGMA synchronous = NORMAL;
                        PRAGMA foreign_keys = ON;
                    ";
                    await pragmaCmd.ExecuteNonQueryAsync();
                }

                // إنشاء الجداول الأساسية
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS Categories (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Name TEXT NOT NULL UNIQUE,
                            NameAr TEXT,
                            Description TEXT,
                            CreatedAt TEXT NOT NULL
                        );

                        CREATE TABLE IF NOT EXISTS Products (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Reference TEXT NOT NULL UNIQUE,
                            Barcode TEXT,
                            Name TEXT NOT NULL,
                            NameAr TEXT,
                            CategoryId INTEGER NOT NULL,
                            Dimensions TEXT,
                            Brand TEXT,
                            PurchasePrice REAL NOT NULL DEFAULT 0,
                            SalePrice REAL NOT NULL DEFAULT 0,
                            WholesalePrice REAL NOT NULL DEFAULT 0,
                            StockQuantity REAL NOT NULL DEFAULT 0,
                            MinStockAlert REAL NOT NULL DEFAULT 5,
                            SaleUnit INTEGER NOT NULL DEFAULT 0,
                            PurchaseUnit INTEGER NOT NULL DEFAULT 0,
                            ConversionFactor REAL NOT NULL DEFAULT 1,
                            Location TEXT,
                            DefaultSupplierId INTEGER,
                            IsActive INTEGER NOT NULL DEFAULT 1,
                            CreatedAt TEXT NOT NULL,
                            UpdatedAt TEXT NOT NULL,
                            FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
                        );

                        CREATE INDEX IF NOT EXISTS idx_products_barcode ON Products(Barcode);
                        CREATE INDEX IF NOT EXISTS idx_products_ref ON Products(Reference);
                        CREATE INDEX IF NOT EXISTS idx_products_name ON Products(Name);

                        CREATE TABLE IF NOT EXISTS Customers (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            FullName TEXT NOT NULL,
                            Phone TEXT,
                            Address TEXT,
                            Activity TEXT,
                            CurrentDebt REAL NOT NULL DEFAULT 0,
                            MaxCreditLimit REAL NOT NULL DEFAULT 50000,
                            AppliesWholesalePrice INTEGER NOT NULL DEFAULT 0,
                            Notes TEXT,
                            CreatedAt TEXT NOT NULL,
                            UpdatedAt TEXT NOT NULL
                        );

                        CREATE TABLE IF NOT EXISTS CustomerPayments (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            CustomerId INTEGER NOT NULL,
                            Amount REAL NOT NULL,
                            PreviousDebt REAL NOT NULL,
                            RemainingDebt REAL NOT NULL,
                            PaymentMethod INTEGER NOT NULL DEFAULT 0,
                            ReferenceNumber TEXT,
                            Notes TEXT,
                            CreatedByUserId INTEGER NOT NULL DEFAULT 1,
                            PaymentDate TEXT NOT NULL,
                            FOREIGN KEY (CustomerId) REFERENCES Customers(Id)
                        );

                        CREATE TABLE IF NOT EXISTS Suppliers (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Name TEXT NOT NULL,
                            CompanyName TEXT,
                            Phone TEXT,
                            Address TEXT,
                            Wilaya TEXT,
                            CurrentDebt REAL NOT NULL DEFAULT 0,
                            Notes TEXT,
                            CreatedAt TEXT NOT NULL
                        );

                        CREATE TABLE IF NOT EXISTS Sales (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            InvoiceNumber TEXT NOT NULL UNIQUE,
                            CustomerId INTEGER,
                            CustomerName TEXT NOT NULL,
                            PaymentType INTEGER NOT NULL DEFAULT 0,
                            SubTotal REAL NOT NULL DEFAULT 0,
                            Discount REAL NOT NULL DEFAULT 0,
                            TotalAmount REAL NOT NULL DEFAULT 0,
                            PaidAmount REAL NOT NULL DEFAULT 0,
                            ChangeAmount REAL NOT NULL DEFAULT 0,
                            DebtAmount REAL NOT NULL DEFAULT 0,
                            ProfitAmount REAL NOT NULL DEFAULT 0,
                            UserId INTEGER NOT NULL DEFAULT 1,
                            UserName TEXT,
                            Notes TEXT,
                            CreatedAt TEXT NOT NULL,
                            FOREIGN KEY (CustomerId) REFERENCES Customers(Id)
                        );

                        CREATE INDEX IF NOT EXISTS idx_sales_date ON Sales(CreatedAt);
                        CREATE INDEX IF NOT EXISTS idx_sales_inv ON Sales(InvoiceNumber);

                        CREATE TABLE IF NOT EXISTS SaleItems (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            SaleId INTEGER NOT NULL,
                            ProductId INTEGER NOT NULL,
                            ProductReference TEXT NOT NULL,
                            ProductName TEXT NOT NULL,
                            Quantity REAL NOT NULL,
                            Unit INTEGER NOT NULL DEFAULT 0,
                            UnitPrice REAL NOT NULL,
                            PurchasePrice REAL NOT NULL,
                            TotalPrice REAL NOT NULL,
                            FOREIGN KEY (SaleId) REFERENCES Sales(Id) ON DELETE CASCADE,
                            FOREIGN KEY (ProductId) REFERENCES Products(Id)
                        );

                        CREATE TABLE IF NOT EXISTS Purchases (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            InvoiceNumber TEXT NOT NULL,
                            SupplierId INTEGER NOT NULL,
                            TotalAmount REAL NOT NULL,
                            PaidAmount REAL NOT NULL,
                            RemainingDebt REAL NOT NULL,
                            Notes TEXT,
                            PurchaseDate TEXT NOT NULL,
                            FOREIGN KEY (SupplierId) REFERENCES Suppliers(Id)
                        );

                        CREATE TABLE IF NOT EXISTS PurchaseItems (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            PurchaseId INTEGER NOT NULL,
                            ProductId INTEGER NOT NULL,
                            ProductName TEXT NOT NULL,
                            Quantity REAL NOT NULL,
                            Unit INTEGER NOT NULL DEFAULT 0,
                            UnitPrice REAL NOT NULL,
                            TotalPrice REAL NOT NULL,
                            FOREIGN KEY (PurchaseId) REFERENCES Purchases(Id) ON DELETE CASCADE
                        );

                        CREATE TABLE IF NOT EXISTS StoreSettings (
                            Id INTEGER PRIMARY KEY,
                            StoreName TEXT NOT NULL,
                            OwnerName TEXT,
                            Phone1 TEXT,
                            Phone2 TEXT,
                            Address TEXT,
                            ReceiptHeader TEXT,
                            ReceiptFooter TEXT,
                            ThermalPrinterName TEXT,
                            ThermalPrinterWidthMm INTEGER NOT NULL DEFAULT 80,
                            AutoPrintReceipt INTEGER NOT NULL DEFAULT 1,
                            AutoOpenCashDrawer INTEGER NOT NULL DEFAULT 1,
                            BackupFolder TEXT,
                            Language TEXT NOT NULL DEFAULT 'fr'
                        );

                        CREATE TABLE IF NOT EXISTS Users (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Username TEXT NOT NULL UNIQUE,
                            PasswordHash TEXT NOT NULL,
                            FullName TEXT NOT NULL,
                            Role INTEGER NOT NULL DEFAULT 0,
                            IsActive INTEGER NOT NULL DEFAULT 1,
                            CreatedAt TEXT NOT NULL
                        );
                    ";
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
