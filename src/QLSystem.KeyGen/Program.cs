using System;
using QLSystem.Licensing;

namespace QLSystem.KeyGen
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("===============================================================");
            Console.WriteLine("        QL SYSTEM - أداة توليد مفاتيح التفعيل الرسمية");
            Console.WriteLine("       Générateur de Clés de Licence (Usage Développeur)      ");
            Console.WriteLine("===============================================================");
            Console.ResetColor();

            while (true)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("أدخل كود جهاز الزبون (Machine ID) المكتوب في الشاشة (أو 'q' للخروج): ");
                Console.ResetColor();

                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input) || input.Trim().ToLower() == "q")
                {
                    break;
                }

                var machineId = input.Trim().ToUpperInvariant();

                try
                {
                    var activationKey = LicenseValidator.GenerateKeyForMachine(machineId);

                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("---------------------------------------------------------------");
                    Console.WriteLine($"[✓] كود الجهاز:   {machineId}");
                    Console.WriteLine($"[★] مفتاح التفعيل: {activationKey}");
                    Console.WriteLine("---------------------------------------------------------------");
                    Console.ResetColor();
                    Console.WriteLine("انسخ هذا المفتاح وابعثه للزبون عبر واتساب أو الهاتف.");
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[!] خطأ أثناء التوليد: {ex.Message}");
                    Console.ResetColor();
                }
            }

            Console.WriteLine("\nمع السلامة!");
        }
    }
}
