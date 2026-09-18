using System;
using System.Threading.Tasks;
using System.Windows.Input;
using QLSystem.App.Helpers;
using QLSystem.Core.Interfaces;
using QLSystem.Core.Models;
using QLSystem.Data.Backup;

namespace QLSystem.App.ViewModels
{
    /// <summary>
    /// إدارة إعدادات المحل، الطباعة والنسخ الاحتياطي (Paramètres)
    /// </summary>
    public class SettingsViewModel : ObservableObject
    {
        private readonly ISettingsRepository _settingsRepository;
        private readonly BackupService _backupService;

        private StoreSettings _settings = new StoreSettings();
        private string _statusMessage = string.Empty;

        public StoreSettings Settings
        {
            get => _settings;
            set => SetProperty(ref _settings, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ICommand SaveSettingsCommand { get; }
        public ICommand BackupNowCommand { get; }

        public SettingsViewModel(ISettingsRepository settingsRepository, BackupService backupService)
        {
            _settingsRepository = settingsRepository;
            _backupService = backupService;

            SaveSettingsCommand = new RelayCommand(async () => await ExecuteSaveSettingsAsync());
            BackupNowCommand = new RelayCommand(ExecuteBackupNow);

            _ = LoadSettingsAsync();
        }

        private async Task LoadSettingsAsync()
        {
            Settings = await _settingsRepository.GetSettingsAsync();
        }

        private async Task ExecuteSaveSettingsAsync()
        {
            var success = await _settingsRepository.SaveSettingsAsync(Settings);
            StatusMessage = success ? "تم حفظ الإعدادات بنجاح!" : "فشل حفظ الإعدادات.";
        }

        private void ExecuteBackupNow()
        {
            var path = _backupService.PerformBackup(Settings.BackupFolder);
            if (!string.IsNullOrEmpty(path))
            {
                StatusMessage = $"تم حفظ النسخة الاحتياطية بنجاح في: {path}";
            }
            else
            {
                StatusMessage = "حدث خطأ أثناء إجراء النسخة الاحتياطية.";
            }
        }
    }
}
