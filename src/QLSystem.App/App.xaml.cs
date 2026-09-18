using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using QLSystem.App.ViewModels;
using QLSystem.Data;
using QLSystem.Data.Seed;
using QLSystem.Licensing;

namespace QLSystem.App
{
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // 1. تهيئة مسارات البيانات
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var dataFolder = Path.Combine(appData, "QLSystem", "data");
                var licenseFolder = Path.Combine(appData, "QLSystem", "license");

                if (!Directory.Exists(dataFolder)) Directory.CreateDirectory(dataFolder);
                if (!Directory.Exists(licenseFolder)) Directory.CreateDirectory(licenseFolder);

                // 2. تهيئة قاعدة البيانات وملء سلع الكانكري الأولية
                var dbContext = new DatabaseContext();
                await dbContext.InitializeAsync();
                await SeedDatabase.SeedAsync(dbContext);

                // 3. تهيئة نظام الـ 7 أيام والتراخيص
                var trialManager = new TrialManager(licenseFolder);

                // 4. بناء الـ ViewModel الرئيسي وفتح النافذة
                var mainViewModel = new MainViewModel(dbContext, trialManager);
                var mainWindow = new MainWindow(mainViewModel);
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء إقلاع البرنامج:\n{ex.Message}\n{ex.StackTrace}", 
                                "خطأ QL System", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }
    }
}
