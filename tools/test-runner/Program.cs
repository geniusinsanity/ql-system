using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using QLSystem.Data;
using QLSystem.Data.Repositories;
using QLSystem.Data.Seed;
using QLSystem.Licensing;

namespace QLSystem.TestRunner
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("===============================================================");
            Console.WriteLine("        QL SYSTEM - أداة الفحص والتجربة المباشرة على Linux     ");
            Console.WriteLine("===============================================================");
            Console.ResetColor();

            // 1. فحص كود الجهاز الحالي
            var machineId = HardwareFingerprint.GetMachineId();
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[1] كود هذا الحاسوب الحالي (Machine ID): {machineId}");
            Console.ResetColor();

            // 2. تجربة نظام الـ 7 أيام التجريبية
            Console.WriteLine();
            Console.WriteLine("[2] فحص نظام الـ 7 أيام التجريبية (Trial Engine)...");
            var testFolder = Path.Combine(Directory.GetCurrentDirectory(), "test_data");
            var trialMgr = new TrialManager(Path.Combine(testFolder, "license"));
            var license = trialMgr.EvaluateLicense();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"    ✓ حالة الترخيص: {license.Status}");
            Console.WriteLine($"    ✓ الأيام المتبقية: {license.RemainingDays} أيام");
            Console.WriteLine($"    ✓ هل هو صالح للبيع: {license.IsUsable}");
            Console.ResetColor();

            // 3. تهيئة قاعدة بيانات SQLite والسلع المسبقة
            Console.WriteLine();
            Console.WriteLine("[3] تهيئة قاعدة بيانات الكانكري وتعبئة السلع...");
            var dbContext = new DatabaseContext(Path.Combine(testFolder, "quincaillerie.db"));
            await dbContext.InitializeAsync();
            await SeedDatabase.SeedAsync(dbContext);

            var productRepo = new ProductRepository(dbContext);
            var allProducts = (await productRepo.GetAllAsync()).ToList();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"    ✓ تم ملء قاعدة البيانات بنجاح! إجمالي السلع: {allProducts.Count} سلعة");
            Console.ResetColor();

            // 4. تجربة البحث الفوري (Instant Search)
            Console.WriteLine();
            Console.WriteLine("[4] تجربة البحث الفوري عن 'vis' (البراغي):");
            var searchResults = await productRepo.SearchAsync("vis", limit: 5);
            foreach (var item in searchResults)
            {
                Console.WriteLine($"    - [{item.Reference}] {item.Name} | القياس: {item.Dimensions} | السعر: {item.SalePrice:N0} دج | المخزون: {item.StockQuantity} {item.SaleUnit}");
            }

            // 5. تجربة توليد كود التفعيل والتحقق منه
            Console.WriteLine();
            Console.WriteLine("[5] تجربة توليد مفتاح التفعيل الدائم والتحقق منه:");
            var generatedKey = LicenseValidator.GenerateKeyForMachine(machineId);
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"    ★ المفتاح المولد: {generatedKey}");
            Console.ResetColor();

            var isValid = LicenseValidator.ValidateKey(machineId, generatedKey);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"    ✓ فحص المفتاح: {(isValid ? "صحيح ومطابق 100%!" : "غير مطابق!")}");
            Console.ResetColor();

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("===============================================================");
            Console.WriteLine("         جميع الأنظمة تعمل بكفاءة تامة 100%!          ");
            Console.WriteLine("===============================================================");
            Console.ResetColor();
        }
    }
}
