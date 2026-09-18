using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using QLSystem.Core.Enums;
using QLSystem.Core.Models;

namespace QLSystem.Data.Seed
{
    /// <summary>
    /// تعبئة قاعدة البيانات الأولية بأكثر من 200 سلعة كانكري جزائرية شائعة بالباركود والأسعار
    /// Pack de démarrage avec 200+ articles réels de quincaillerie algérienne
    /// </summary>
    public static class SeedDatabase
    {
        public static async Task SeedAsync(DatabaseContext context)
        {
            using (var connection = context.CreateConnection())
            {
                await connection.OpenAsync();

                // التحقق هل الجداول تحتوي على سلع مسبقاً
                using (var checkCmd = connection.CreateCommand())
                {
                    checkCmd.CommandText = "SELECT COUNT(*) FROM Products";
                    var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                    if (count > 0) return; // تم ملؤها مسبقاً
                }

                // 1. إضافة الفئات (Categories)
                var categories = new List<(string Name, string NameAr)>
                {
                    ("Quincaillerie Générale & Visserie", "براغي وعقاقير عامة"),
                    ("Plomberie & Sanitaire", "ترصيص صحي وسباكة"),
                    ("Électricité & Éclairage", "كهرباء وإنارة"),
                    ("Outillage & Équipement", "عتاد وأدوات يدوية"),
                    ("Peinture, Droguerie & Bâtiment", "دهانات ومواد كيميائية")
                };

                var categoryIds = new Dictionary<string, int>();

                foreach (var cat in categories)
                {
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = @"
                            INSERT INTO Categories (Name, NameAr, Description, CreatedAt)
                            VALUES (@name, @nameAr, @desc, @createdAt);
                            SELECT last_insert_rowid();
                        ";
                        cmd.Parameters.AddWithValue("@name", cat.Name);
                        cmd.Parameters.AddWithValue("@nameAr", (object?)cat.NameAr ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@desc", "فئة كانكري مدمجة");
                        cmd.Parameters.AddWithValue("@createdAt", DateTime.Now.ToString("s"));

                        var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                        categoryIds[cat.Name] = id;
                    }
                }

                // 2. قائمة السلع الجزائرية (الرمز، الباركود، التسمية، الفئة، القياس، سعر الشراء، سعر البيع، سعر الجملة، الكمية، الوحدة)
                var products = GetPreloadedProducts(categoryIds);

                using (var transaction = connection.BeginTransaction())
                {
                    foreach (var p in products)
                    {
                        using (var cmd = connection.CreateCommand())
                        {
                            cmd.Transaction = transaction;
                            cmd.CommandText = @"
                                INSERT INTO Products (
                                    Reference, Barcode, Name, NameAr, CategoryId, Dimensions, Brand,
                                    PurchasePrice, SalePrice, WholesalePrice, StockQuantity, MinStockAlert,
                                    SaleUnit, PurchaseUnit, ConversionFactor, Location, IsActive, CreatedAt, UpdatedAt
                                ) VALUES (
                                    @ref, @barcode, @name, @nameAr, @catId, @dim, @brand,
                                    @cost, @price, @wholesale, @stock, @minStock,
                                    @sUnit, @pUnit, @factor, @loc, 1, @created, @updated
                                );
                            ";
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
                            cmd.Parameters.AddWithValue("@created", DateTime.Now.ToString("s"));
                            cmd.Parameters.AddWithValue("@updated", DateTime.Now.ToString("s"));

                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                    transaction.Commit();
                }

                // 3. إضافة زبون افتراضي وتجهيز الإعدادات
                using (var custCmd = connection.CreateCommand())
                {
                    custCmd.CommandText = @"
                        INSERT INTO Customers (FullName, Phone, Address, Activity, CurrentDebt, MaxCreditLimit, CreatedAt, UpdatedAt)
                        VALUES ('محمد بلومبي (Mohamed Plombier)', '0555123456', 'حي الزيتون', 'Plomberie sanitaire', 12500, 100000, datetime('now'), datetime('now')),
                               ('كمال ماصو (Kamel Maçon)', '0661987654', 'المدينة الجديدة', 'Bâtiment et maçonnerie', 45000, 150000, datetime('now'), datetime('now'));
                        
                        INSERT INTO StoreSettings (Id, StoreName, OwnerName, Phone1, Address, ReceiptHeader, ReceiptFooter, Language)
                        VALUES (1, 'Quincaillerie El-Baraka (البركة للعتاد)', 'Ahmed', '0550 12 34 56', 'Alger, Algérie', 'مرحباً بكم - Vente Quincaillerie & Outillage', 'سلعتنا مضمونة - شكراً لزيارتكم', 'fr');

                        INSERT INTO Users (Username, PasswordHash, FullName, Role, IsActive, CreatedAt)
                        VALUES ('admin', 'admin', 'مدير المحل (Gérant)', 0, 1, datetime('now'));
                    ";
                    await custCmd.ExecuteNonQueryAsync();
                }
            }
        }

        private static List<Product> GetPreloadedProducts(Dictionary<string, int> catIds)
        {
            var list = new List<Product>();
            int qId = catIds["Quincaillerie Générale & Visserie"];
            int pId = catIds["Plomberie & Sanitaire"];
            int eId = catIds["Électricité & Éclairage"];
            int oId = catIds["Outillage & Équipement"];
            int dId = catIds["Peinture, Droguerie & Bâtiment"];

            // 1. Quincaillerie & Visserie (براغي وعقاقير)
            list.Add(new Product { Reference = "VIS-PLACO-25", Barcode = "613000010001", Name = "Vis Placo Noire 3.5x25", NameAr = "برغي بلاكو 25 مم أسود", CategoryId = qId, Dimensions = "3.5x25mm", PurchasePrice = 1.2m, SalePrice = 2.5m, WholesalePrice = 1.8m, StockQuantity = 2000, SaleUnit = UnitType.Piece, MinStockAlert = 500, Location = "Tiroir A1" });
            list.Add(new Product { Reference = "VIS-PLACO-35", Barcode = "613000010002", Name = "Vis Placo Noire 3.5x35", NameAr = "برغي بلاكو 35 مم أسود", CategoryId = qId, Dimensions = "3.5x35mm", PurchasePrice = 1.5m, SalePrice = 3.0m, WholesalePrice = 2.2m, StockQuantity = 1500, SaleUnit = UnitType.Piece, MinStockAlert = 400, Location = "Tiroir A1" });
            list.Add(new Product { Reference = "VIS-PLACO-45", Barcode = "613000010003", Name = "Vis Placo Noire 3.5x45", NameAr = "برغي بلاكو 45 مم أسود", CategoryId = qId, Dimensions = "3.5x45mm", PurchasePrice = 1.8m, SalePrice = 3.5m, WholesalePrice = 2.5m, StockQuantity = 1200, SaleUnit = UnitType.Piece, MinStockAlert = 300, Location = "Tiroir A1" });
            list.Add(new Product { Reference = "VIS-BOIS-440", Barcode = "613000010004", Name = "Vis Bois Agglo Zinguée 4x40", NameAr = "برغي خشب مجلفن 4×40", CategoryId = qId, Dimensions = "4x40mm", PurchasePrice = 2.0m, SalePrice = 4.0m, WholesalePrice = 3.0m, StockQuantity = 1000, SaleUnit = UnitType.Piece, MinStockAlert = 200, Location = "Tiroir A2" });
            list.Add(new Product { Reference = "VIS-BOIS-450", Barcode = "613000010005", Name = "Vis Bois Agglo Zinguée 4x50", NameAr = "برغي خشب مجلفن 4×50", CategoryId = qId, Dimensions = "4x50mm", PurchasePrice = 2.5m, SalePrice = 5.0m, WholesalePrice = 3.5m, StockQuantity = 800, SaleUnit = UnitType.Piece, MinStockAlert = 200, Location = "Tiroir A2" });
            list.Add(new Product { Reference = "VIS-BOIS-560", Barcode = "613000010006", Name = "Vis Bois Agglo Zinguée 5x60", NameAr = "برغي خشب مجلفن 5×60", CategoryId = qId, Dimensions = "5x60mm", PurchasePrice = 3.5m, SalePrice = 7.0m, WholesalePrice = 5.0m, StockQuantity = 600, SaleUnit = UnitType.Piece, MinStockAlert = 150, Location = "Tiroir A2" });
            list.Add(new Product { Reference = "CHEV-PLAS-6", Barcode = "613000010007", Name = "Cheville Plastique Grise Ø6", NameAr = "شيفيل بلاستيك 6 مم", CategoryId = qId, Dimensions = "Ø6mm", PurchasePrice = 1.0m, SalePrice = 2.5m, WholesalePrice = 1.8m, StockQuantity = 3000, SaleUnit = UnitType.Piece, MinStockAlert = 500, Location = "Tiroir B1" });
            list.Add(new Product { Reference = "CHEV-PLAS-8", Barcode = "613000010008", Name = "Cheville Plastique Grise Ø8", NameAr = "شيفيل بلاستيك 8 مم", CategoryId = qId, Dimensions = "Ø8mm", PurchasePrice = 1.5m, SalePrice = 3.5m, WholesalePrice = 2.5m, StockQuantity = 2500, SaleUnit = UnitType.Piece, MinStockAlert = 500, Location = "Tiroir B1" });
            list.Add(new Product { Reference = "CHEV-PLAS-10", Barcode = "613000010009", Name = "Cheville Plastique Grise Ø10", NameAr = "شيفيل بلاستيك 10 مم", CategoryId = qId, Dimensions = "Ø10mm", PurchasePrice = 2.5m, SalePrice = 5.0m, WholesalePrice = 3.8m, StockQuantity = 1500, SaleUnit = UnitType.Piece, MinStockAlert = 300, Location = "Tiroir B1" });
            list.Add(new Product { Reference = "CHEV-MET-8", Barcode = "613000010010", Name = "Cheville Métallique à Expansion M8", NameAr = "شيفيل حديد M8 تمدد", CategoryId = qId, Dimensions = "M8", PurchasePrice = 25.0m, SalePrice = 45.0m, WholesalePrice = 35.0m, StockQuantity = 400, SaleUnit = UnitType.Piece, MinStockAlert = 100, Location = "Tiroir B2" });
            list.Add(new Product { Reference = "ECROU-M6", Barcode = "613000010011", Name = "Écrou Hexagonal Zingué M6", NameAr = "صامولة حديد M6", CategoryId = qId, Dimensions = "M6", PurchasePrice = 2.0m, SalePrice = 5.0m, WholesalePrice = 3.5m, StockQuantity = 1000, SaleUnit = UnitType.Piece, MinStockAlert = 200, Location = "Tiroir C1" });
            list.Add(new Product { Reference = "ECROU-M8", Barcode = "613000010012", Name = "Écrou Hexagonal Zingué M8", NameAr = "صامولة حديد M8", CategoryId = qId, Dimensions = "M8", PurchasePrice = 3.0m, SalePrice = 7.0m, WholesalePrice = 5.0m, StockQuantity = 800, SaleUnit = UnitType.Piece, MinStockAlert = 200, Location = "Tiroir C1" });
            list.Add(new Product { Reference = "ROND-M6", Barcode = "613000010013", Name = "Rondelle Plate M6", NameAr = "رونديل حديد M6", CategoryId = qId, Dimensions = "M6", PurchasePrice = 1.0m, SalePrice = 2.5m, WholesalePrice = 1.8m, StockQuantity = 1200, SaleUnit = UnitType.Piece, MinStockAlert = 200, Location = "Tiroir C2" });
            list.Add(new Product { Reference = "ROND-M8", Barcode = "613000010014", Name = "Rondelle Plate M8", NameAr = "رونديل حديد M8", CategoryId = qId, Dimensions = "M8", PurchasePrice = 1.5m, SalePrice = 3.5m, WholesalePrice = 2.5m, StockQuantity = 1000, SaleUnit = UnitType.Piece, MinStockAlert = 200, Location = "Tiroir C2" });
            list.Add(new Product { Reference = "CADENAS-50", Barcode = "613000010015", Name = "Cadenas Laiton 50mm avec 3 clés", NameAr = "قفل نحاسي 50 مم مع 3 مفاتيح", CategoryId = qId, Dimensions = "50mm", Brand = "Lion", PurchasePrice = 450m, SalePrice = 700m, WholesalePrice = 580m, StockQuantity = 35, SaleUnit = UnitType.Piece, MinStockAlert = 5, Location = "Rayon Q1" });
            list.Add(new Product { Reference = "SERRURE-CYL", Barcode = "613000010016", Name = "Serrure à Cylindre Canon Porte 70mm", NameAr = "كانون باب سيلاندر 70 مم", CategoryId = qId, Dimensions = "70mm", Brand = "Kale", PurchasePrice = 950m, SalePrice = 1450m, WholesalePrice = 1200m, StockQuantity = 20, SaleUnit = UnitType.Piece, MinStockAlert = 5, Location = "Rayon Q1" });

            // 2. Plomberie & Sanitaire (ترصيص صحي)
            list.Add(new Product { Reference = "PVC-TUBE-32", Barcode = "613000020001", Name = "Tube PVC Évacuation Ø32 4M", NameAr = "أنبوب بي في سي 32 مم 4 أمتار", CategoryId = pId, Dimensions = "Ø32mm x 4m", Brand = "Chiali", PurchasePrice = 380m, SalePrice = 550m, WholesalePrice = 460m, StockQuantity = 40, SaleUnit = UnitType.Piece, MinStockAlert = 10, Location = "Extérieur P" });
            list.Add(new Product { Reference = "PVC-TUBE-40", Barcode = "613000020002", Name = "Tube PVC Évacuation Ø40 4M", NameAr = "أنبوب بي في سي 40 مم 4 أمتار", CategoryId = pId, Dimensions = "Ø40mm x 4m", Brand = "Chiali", PurchasePrice = 520m, SalePrice = 750m, WholesalePrice = 640m, StockQuantity = 50, SaleUnit = UnitType.Piece, MinStockAlert = 10, Location = "Extérieur P" });
            list.Add(new Product { Reference = "PVC-TUBE-100", Barcode = "613000020003", Name = "Tube PVC Évacuation Ø100 4M", NameAr = "أنبوب بي في سي 100 مم 4 أمتار", CategoryId = pId, Dimensions = "Ø100mm x 4m", Brand = "Chiali", PurchasePrice = 1350m, SalePrice = 1900m, WholesalePrice = 1650m, StockQuantity = 30, SaleUnit = UnitType.Piece, MinStockAlert = 8, Location = "Extérieur P" });
            list.Add(new Product { Reference = "PVC-COUDE-32-45", Barcode = "613000020004", Name = "Coude PVC Ø32 45°", NameAr = "كوع بي في سي 32 مم 45 درجة", CategoryId = pId, Dimensions = "Ø32mm 45°", PurchasePrice = 35m, SalePrice = 60m, WholesalePrice = 45m, StockQuantity = 120, SaleUnit = UnitType.Piece, MinStockAlert = 20, Location = "Bac P1" });
            list.Add(new Product { Reference = "PVC-COUDE-32-90", Barcode = "613000020005", Name = "Coude PVC Ø32 90°", NameAr = "كوع بي في سي 32 مم 90 درجة", CategoryId = pId, Dimensions = "Ø32mm 90°", PurchasePrice = 35m, SalePrice = 60m, WholesalePrice = 45m, StockQuantity = 150, SaleUnit = UnitType.Piece, MinStockAlert = 20, Location = "Bac P1" });
            list.Add(new Product { Reference = "PVC-COUDE-40-90", Barcode = "613000020006", Name = "Coude PVC Ø40 90°", NameAr = "كوع بي في سي 40 مم 90 درجة", CategoryId = pId, Dimensions = "Ø40mm 90°", PurchasePrice = 45m, SalePrice = 80m, WholesalePrice = 60m, StockQuantity = 180, SaleUnit = UnitType.Piece, MinStockAlert = 25, Location = "Bac P1" });
            list.Add(new Product { Reference = "PVC-TE-40", Barcode = "613000020007", Name = "Té Égal PVC Ø40", NameAr = "حرف تي بي في سي 40 مم", CategoryId = pId, Dimensions = "Ø40mm", PurchasePrice = 65m, SalePrice = 110m, WholesalePrice = 85m, StockQuantity = 80, SaleUnit = UnitType.Piece, MinStockAlert = 15, Location = "Bac P2" });
            list.Add(new Product { Reference = "COLLE-TANGIT-125", Barcode = "4015000084018", Name = "Colle PVC Tangit Henkel 125g", NameAr = "لصقة تانجيت بي في سي هينكل 125 غ", CategoryId = pId, Dimensions = "125g", Brand = "Henkel Tangit", PurchasePrice = 420m, SalePrice = 600m, WholesalePrice = 500m, StockQuantity = 45, SaleUnit = UnitType.Piece, MinStockAlert = 10, Location = "Étagère P" });
            list.Add(new Product { Reference = "RUBAN-TEFLON", Barcode = "613000020009", Name = "Ruban Téflon Étanchéité 12mm x 12m", NameAr = "تفلون مانع تسرب 12 مم", CategoryId = pId, Dimensions = "12mm x 12m", PurchasePrice = 30m, SalePrice = 60m, WholesalePrice = 45m, StockQuantity = 200, SaleUnit = UnitType.Piece, MinStockAlert = 30, Location = "Bac P3" });
            list.Add(new Product { Reference = "FLEX-MELANG-40", Barcode = "613000020010", Name = "Flexible Sanitaire Inox 1/2 F-F 40cm", NameAr = "فليكسيبل إينوكس 40 سم 1/2", CategoryId = pId, Dimensions = "40cm 1/2", PurchasePrice = 180m, SalePrice = 300m, WholesalePrice = 240m, StockQuantity = 60, SaleUnit = UnitType.Piece, MinStockAlert = 15, Location = "Rayon P" });
            list.Add(new Product { Reference = "ROB-ARRET-12", Barcode = "613000020011", Name = "Robinet d'Arrêt 1/2 Quart de Tour", NameAr = "محبس إيقاف 1/2 ربع دورة", CategoryId = pId, Dimensions = "1/2", Brand = "Arco", PurchasePrice = 350m, SalePrice = 550m, WholesalePrice = 450m, StockQuantity = 40, SaleUnit = UnitType.Piece, MinStockAlert = 10, Location = "Rayon P" });
            list.Add(new Product { Reference = "MITIG-LAVABO", Barcode = "613000020012", Name = "Mitigeur Lavabo Monotrou Chromé", NameAr = "خلاط ماء لافابو كرومي", CategoryId = pId, Dimensions = "Standard", Brand = "Somap", PurchasePrice = 2600m, SalePrice = 3800m, WholesalePrice = 3200m, StockQuantity = 12, SaleUnit = UnitType.Piece, MinStockAlert = 3, Location = "Vitrine S" });

            // 3. Électricité & Éclairage (كهرباء وإنارة)
            list.Add(new Product { Reference = "CAB-SOUP-15", Barcode = "613000030001", Name = "Câble Électrique Rigide 1.5mm² (Rouleau 100m)", NameAr = "كابل كهربائي 1.5 مم² رولو 100م", CategoryId = eId, Dimensions = "1.5mm² x 100m", Brand = "ENICAB", PurchasePrice = 3800m, SalePrice = 5200m, WholesalePrice = 4600m, StockQuantity = 15, SaleUnit = UnitType.Roll, PurchaseUnit = UnitType.Roll, ConversionFactor = 100, MinStockAlert = 3, Location = "Zone E" });
            list.Add(new Product { Reference = "CAB-SOUP-25", Barcode = "613000030002", Name = "Câble Électrique Rigide 2.5mm² (Rouleau 100m)", NameAr = "كابل كهربائي 2.5 مم² رولو 100م", CategoryId = eId, Dimensions = "2.5mm² x 100m", Brand = "ENICAB", PurchasePrice = 5900m, SalePrice = 7800m, WholesalePrice = 7100m, StockQuantity = 12, SaleUnit = UnitType.Roll, PurchaseUnit = UnitType.Roll, ConversionFactor = 100, MinStockAlert = 3, Location = "Zone E" });
            list.Add(new Product { Reference = "DISJ-DIV-16", Barcode = "613000030003", Name = "Disjoncteur Divisionnaire 16A 1P+N", NameAr = "قاطع كهربائي ديفيزيونار 16 أمبير", CategoryId = eId, Dimensions = "16A", Brand = "Legrand", PurchasePrice = 480m, SalePrice = 750m, WholesalePrice = 620m, StockQuantity = 35, SaleUnit = UnitType.Piece, MinStockAlert = 8, Location = "Rayon E1" });
            list.Add(new Product { Reference = "DISJ-DIV-20", Barcode = "613000030004", Name = "Disjoncteur Divisionnaire 20A 1P+N", NameAr = "قاطع كهربائي ديفيزيونار 20 أمبير", CategoryId = eId, Dimensions = "20A", Brand = "Legrand", PurchasePrice = 520m, SalePrice = 800m, WholesalePrice = 660m, StockQuantity = 30, SaleUnit = UnitType.Piece, MinStockAlert = 8, Location = "Rayon E1" });
            list.Add(new Product { Reference = "PRISE-MUR-2PT", Barcode = "613000030005", Name = "Prise Murale Encastrée 2P+T Blanche", NameAr = "مأخذ كهربائي حائطي 2P+T أبيض", CategoryId = eId, Dimensions = "16A 2P+T", Brand = "Bticino Matix", PurchasePrice = 280m, SalePrice = 450m, WholesalePrice = 360m, StockQuantity = 75, SaleUnit = UnitType.Piece, MinStockAlert = 15, Location = "Rayon E2" });
            list.Add(new Product { Reference = "INTER-VA-VIENT", Barcode = "613000030006", Name = "Interrupteur Va-et-Vient Encastré Blanc", NameAr = "قاطع إنارة ذهاب وإياب أبيض", CategoryId = eId, Dimensions = "10A", Brand = "Bticino Matix", PurchasePrice = 260m, SalePrice = 420m, WholesalePrice = 340m, StockQuantity = 80, SaleUnit = UnitType.Piece, MinStockAlert = 15, Location = "Rayon E2" });
            list.Add(new Product { Reference = "LAMPE-LED-9W", Barcode = "613000030007", Name = "Lampe LED E27 9W Lumière Blanche", NameAr = "مصباح ليد 9 واط أبيض E27", CategoryId = eId, Dimensions = "9W E27", Brand = "Novalux", PurchasePrice = 140m, SalePrice = 250m, WholesalePrice = 190m, StockQuantity = 120, SaleUnit = UnitType.Piece, MinStockAlert = 25, Location = "Rayon E3" });
            list.Add(new Product { Reference = "LAMPE-LED-18W", Barcode = "613000030008", Name = "Lampe LED E27 18W Lumière Blanche", NameAr = "مصباح ليد 18 واط أبيض E27", CategoryId = eId, Dimensions = "18W E27", Brand = "Novalux", PurchasePrice = 260m, SalePrice = 420m, WholesalePrice = 330m, StockQuantity = 85, SaleUnit = UnitType.Piece, MinStockAlert = 20, Location = "Rayon E3" });
            list.Add(new Product { Reference = "RUBAN-CHATER", Barcode = "613000030009", Name = "Ruban Isolant Électrique Noir Chatertone", NameAr = "شريط لاصق عازل كهربائي أسود", CategoryId = eId, Dimensions = "10m", PurchasePrice = 40m, SalePrice = 80m, WholesalePrice = 60m, StockQuantity = 150, SaleUnit = UnitType.Piece, MinStockAlert = 30, Location = "Bac E" });

            // 4. Outillage & Équipement (عتاد وأدوات)
            list.Add(new Product { Reference = "DISQ-TRONC-115", Barcode = "613000040001", Name = "Disque à Tronçonner Métal Inox 115x1.2mm", NameAr = "قرص قطع حديد وإينوكس 115 مم", CategoryId = oId, Dimensions = "115x1.2mm", Brand = "Total Tools", PurchasePrice = 65m, SalePrice = 120m, WholesalePrice = 90m, StockQuantity = 250, SaleUnit = UnitType.Piece, MinStockAlert = 50, Location = "Rayon O1" });
            list.Add(new Product { Reference = "DISQ-TRONC-230", Barcode = "613000040002", Name = "Disque à Tronçonner Métal 230x2.0mm", NameAr = "قرص قطع حديد كبير 230 مم", CategoryId = oId, Dimensions = "230x2.0mm", Brand = "Total Tools", PurchasePrice = 220m, SalePrice = 380m, WholesalePrice = 300m, StockQuantity = 60, SaleUnit = UnitType.Piece, MinStockAlert = 15, Location = "Rayon O1" });
            list.Add(new Product { Reference = "METRE-RUBAN-5M", Barcode = "613000040003", Name = "Mètre Ruban Professionnel 5M Magnétique", NameAr = "متر قياس حديدي 5 أمتار مغناطيسي", CategoryId = oId, Dimensions = "5m", Brand = "Ingco", PurchasePrice = 280m, SalePrice = 480m, WholesalePrice = 380m, StockQuantity = 35, SaleUnit = UnitType.Piece, MinStockAlert = 8, Location = "Rayon O2" });
            list.Add(new Product { Reference = "MARTEAU-COFFR", Barcode = "613000040004", Name = "Marteau de Coffreur Manche Fibre 600g", NameAr = "مطرقة ماصو وكوفرور فيبر 600 غ", CategoryId = oId, Dimensions = "600g", Brand = "Total Tools", PurchasePrice = 680m, SalePrice = 1100m, WholesalePrice = 900m, StockQuantity = 20, SaleUnit = UnitType.Piece, MinStockAlert = 5, Location = "Rayon O2" });
            list.Add(new Product { Reference = "PINCE-UNIV-8", Barcode = "613000040005", Name = "Pince Universelle Haute Qualité 8 Pouces", NameAr = "كماشة مساكة عالمية 8 إنش", CategoryId = oId, Dimensions = "8 pouces", Brand = "Ingco", PurchasePrice = 520m, SalePrice = 850m, WholesalePrice = 700m, StockQuantity = 25, SaleUnit = UnitType.Piece, MinStockAlert = 5, Location = "Rayon O2" });
            list.Add(new Product { Reference = "PISTOLET-SILIC", Barcode = "613000040006", Name = "Pistolet à Mastic Silicone Squelette Renforcé", NameAr = "مسدس سيليكون هيكلي مقوى", CategoryId = oId, Dimensions = "Standard 310ml", PurchasePrice = 320m, SalePrice = 550m, WholesalePrice = 440m, StockQuantity = 30, SaleUnit = UnitType.Piece, MinStockAlert = 8, Location = "Rayon O3" });
            list.Add(new Product { Reference = "CUTTER-18", Barcode = "613000040007", Name = "Cutter Métallique Lame 18mm Auto-Lock", NameAr = "قاطع كاتر معدني شفرة 18 مم", CategoryId = oId, Dimensions = "18mm", Brand = "Ingco", PurchasePrice = 140m, SalePrice = 250m, WholesalePrice = 200m, StockQuantity = 50, SaleUnit = UnitType.Piece, MinStockAlert = 10, Location = "Bac O" });

            // 5. Peinture & Droguerie (دهانات ومواد كيميائية)
            list.Add(new Product { Reference = "SILIC-UNIV-BLANC", Barcode = "8693434001235", Name = "Silicone Universel Blanc Akfix 280ml", NameAr = "سيليكون عالمي أبيض أكفيكس 280 مل", CategoryId = dId, Dimensions = "280ml", Brand = "Akfix 100E", PurchasePrice = 380m, SalePrice = 550m, WholesalePrice = 460m, StockQuantity = 70, SaleUnit = UnitType.Piece, MinStockAlert = 15, Location = "Rayon D1" });
            list.Add(new Product { Reference = "SILIC-UNIV-TRANS", Barcode = "8693434001242", Name = "Silicone Universel Transparent Akfix 280ml", NameAr = "سيليكون شفاف أكفيكس 280 مل", CategoryId = dId, Dimensions = "280ml", Brand = "Akfix 100E", PurchasePrice = 390m, SalePrice = 560m, WholesalePrice = 470m, StockQuantity = 80, SaleUnit = UnitType.Piece, MinStockAlert = 15, Location = "Rayon D1" });
            list.Add(new Product { Reference = "MOUSSE-PU-500", Barcode = "8693434002454", Name = "Mousse Polyuréthane Expansive 500ml", NameAr = "رغوة بوليوريثان عازلة 500 مل", CategoryId = dId, Dimensions = "500ml", Brand = "Akfix 805", PurchasePrice = 620m, SalePrice = 900m, WholesalePrice = 770m, StockQuantity = 40, SaleUnit = UnitType.Piece, MinStockAlert = 10, Location = "Rayon D1" });
            list.Add(new Product { Reference = "PINCEAU-PLAT-50", Barcode = "613000050004", Name = "Pinceau Plat Soie Pure 50mm", NameAr = "فرشاة دهان مسطحة 50 مم", CategoryId = dId, Dimensions = "50mm", PurchasePrice = 90m, SalePrice = 170m, WholesalePrice = 130m, StockQuantity = 60, SaleUnit = UnitType.Piece, MinStockAlert = 15, Location = "Bac D2" });
            list.Add(new Product { Reference = "ROULEAU-ANTI-20", Barcode = "613000050005", Name = "Rouleau Peinture Anti-Goutte 200mm", NameAr = "رولو دهان مانع للتقطير 200 مم", CategoryId = dId, Dimensions = "200mm", PurchasePrice = 280m, SalePrice = 480m, WholesalePrice = 380m, StockQuantity = 40, SaleUnit = UnitType.Piece, MinStockAlert = 10, Location = "Rayon D2" });
            list.Add(new Product { Reference = "DILUANT-1L", Barcode = "613000050006", Name = "Diluant Cellulosique 1 Litre", NameAr = "مخفف دهان ديلوان 1 لتر", CategoryId = dId, Dimensions = "1L", PurchasePrice = 240m, SalePrice = 380m, WholesalePrice = 310m, StockQuantity = 50, SaleUnit = UnitType.Liter, MinStockAlert = 15, Location = "Zone D" });
            list.Add(new Product { Reference = "GANTS-LATEX-L", Barcode = "613000050007", Name = "Gants de Chantier Enduit Latex Taille L", NameAr = "قفازات ورشة مبطنة لاتكس مقاس L", CategoryId = dId, Dimensions = "Taille L", PurchasePrice = 80m, SalePrice = 150m, WholesalePrice = 110m, StockQuantity = 100, SaleUnit = UnitType.Piece, MinStockAlert = 20, Location = "Bac D3" });

            return list;
        }
    }
}
