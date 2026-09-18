using System;
using System.Windows;
using System.Windows.Input;
using QLSystem.App.Helpers;
using QLSystem.Core.Enums;
using QLSystem.Core.Interfaces;
using QLSystem.Core.Models;
using QLSystem.Data;
using QLSystem.Data.Backup;
using QLSystem.Data.Repositories;
using QLSystem.Licensing;

namespace QLSystem.App.ViewModels
{
    /// <summary>
    /// إدارة التنقل وحالة البرنامج الإجمالية (Main ViewModel)
    /// </summary>
    public class MainViewModel : ObservableObject
    {
        private readonly DatabaseContext _dbContext;
        private readonly TrialManager _trialManager;
        private readonly BackupService _backupService;

        private readonly IProductRepository _productRepository;
        private readonly ISaleRepository _saleRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly ISupplierRepository _supplierRepository;
        private readonly ISettingsRepository _settingsRepository;

        private object _currentViewModel;
        private LicenseInfo _licenseInfo;
        private string _licenseBadgeText = string.Empty;
        private string _licenseBadgeColor = "#10b981";
        private string _licenseIcon = "✅";
        private string _licenseShortText = "مفعل";
        private bool _isLocked;

        public object CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        public LicenseInfo LicenseInfo
        {
            get => _licenseInfo;
            set => SetProperty(ref _licenseInfo, value);
        }

        public string LicenseBadgeText
        {
            get => _licenseBadgeText;
            set => SetProperty(ref _licenseBadgeText, value);
        }

        public string LicenseBadgeColor
        {
            get => _licenseBadgeColor;
            set => SetProperty(ref _licenseBadgeColor, value);
        }

        public string LicenseIcon
        {
            get => _licenseIcon;
            set => SetProperty(ref _licenseIcon, value);
        }

        public string LicenseShortText
        {
            get => _licenseShortText;
            set => SetProperty(ref _licenseShortText, value);
        }

        public bool IsLocked
        {
            get => _isLocked;
            set => SetProperty(ref _isLocked, value);
        }

        public ICommand NavigateCaisseCommand { get; }
        public ICommand NavigateStockCommand { get; }
        public ICommand NavigateCreditCommand { get; }
        public ICommand NavigateReportsCommand { get; }
        public ICommand NavigateSettingsCommand { get; }
        public ICommand OpenLicenseModalCommand { get; }

        public MainViewModel(DatabaseContext dbContext, TrialManager trialManager)
        {
            _dbContext = dbContext;
            _trialManager = trialManager;
            _backupService = new BackupService(dbContext);

            _productRepository = new ProductRepository(dbContext);
            _saleRepository = new SaleRepository(dbContext);
            _customerRepository = new CustomerRepository(dbContext);
            _supplierRepository = new SupplierRepository(dbContext);
            _settingsRepository = new SettingsRepository(dbContext);

            NavigateCaisseCommand = new RelayCommand(() => SwitchView(new CaisseViewModel(_productRepository, _saleRepository, _customerRepository, _settingsRepository)));
            NavigateStockCommand = new RelayCommand(() => SwitchView(new StockViewModel(_productRepository)));
            NavigateCreditCommand = new RelayCommand(() => SwitchView(new CreditViewModel(_customerRepository)));
            NavigateReportsCommand = new RelayCommand(() => SwitchView(new ReportsViewModel(_saleRepository)));
            NavigateSettingsCommand = new RelayCommand(() => SwitchView(new SettingsViewModel(_settingsRepository, _backupService)));
            OpenLicenseModalCommand = new RelayCommand(ExecuteOpenLicenseModal);

            // تقييم حالة الترخيص
            _licenseInfo = _trialManager.EvaluateLicense();

            if (!_licenseInfo.IsUsable)
            {
                IsLocked = true;
                _currentViewModel = new TrialLockViewModel(_trialManager, OnActivated);
                UpdateLicenseBadge();
            }
            else
            {
                IsLocked = false;
                _currentViewModel = new CaisseViewModel(_productRepository, _saleRepository, _customerRepository, _settingsRepository);
                UpdateLicenseBadge();
            }
        }

        private void SwitchView(object vm)
        {
            if (!IsLocked)
            {
                CurrentViewModel = vm;
            }
        }

        private void OnActivated()
        {
            IsLocked = false;
            LicenseInfo = _trialManager.EvaluateLicense();
            UpdateLicenseBadge();
            CurrentViewModel = new CaisseViewModel(_productRepository, _saleRepository, _customerRepository, _settingsRepository);
        }

        private void ExecuteOpenLicenseModal()
        {
            // Show a simple message box with license details
            var info = _trialManager.EvaluateLicense();
            if (info.Status == LicenseStatus.Activated)
            {
                MessageBox.Show(
                    $"✅ البرنامج مفعل رسمياً ومدى الحياة\n\n" +
                    $"معرف الجهاز: {info.MachineId}\n" +
                    $"مفتاح التفعيل: {info.ActivationKey ?? "—"}\n" +
                    $"نوع الرخصة: دائمة (Illimitée)",
                    "حالة الترخيص — QL System",
                    MessageBoxButton.OK,
                    MessageBoxImage.None);
            }
            else
            {
                var days = info.RemainingDays;
                MessageBox.Show(
                    $"⏳ الفترة التجريبية — باقي {days} أيام\n\n" +
                    $"معرف جهازك (Machine ID):\n{info.MachineId}\n\n" +
                    $"للحصول على مفتاح التفعيل اتصل بالمطور.",
                    "حالة الترخيص — QL System",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void UpdateLicenseBadge()
        {
            if (LicenseInfo?.Status == LicenseStatus.Activated)
            {
                LicenseBadgeText = "نسخة مفعلة رسمياً (Licence Permanente)";
                LicenseBadgeColor = "#10b981";
                LicenseIcon = "✅";
                LicenseShortText = "مفعل";
            }
            else if (LicenseInfo == null || !LicenseInfo.IsUsable)
            {
                LicenseBadgeText = "انتهت فترة التجربة — اتصل بالمطور للتفعيل";
                LicenseBadgeColor = "#ef4444";
                LicenseIcon = "🔒";
                LicenseShortText = "انتهت التجربة";
            }
            else
            {
                var days = LicenseInfo.RemainingDays;
                LicenseBadgeText = $"فترة تجريبية: باقي {days} أيام";
                LicenseBadgeColor = days <= 2 ? "#ef4444" : "#f59e0b";
                LicenseIcon = "⏳";
                LicenseShortText = $"تجريبي • {days}ي";
            }
        }
    }
}
