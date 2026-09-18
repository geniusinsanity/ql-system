using System;
using System.Threading.Tasks;
using System.Windows.Input;
using QLSystem.App.Helpers;
using QLSystem.Core.DTOs;
using QLSystem.Core.Interfaces;

namespace QLSystem.App.ViewModels
{
    /// <summary>
    /// إدارة التقارير والأرباح اليومية والشهرية (Rapports & Statistiques)
    /// </summary>
    public class ReportsViewModel : ObservableObject
    {
        private readonly ISaleRepository _saleRepository;

        private DateTime _selectedDate = DateTime.Today;
        private DailyReportDto _report = new DailyReportDto();
        private bool _isLoading;

        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (SetProperty(ref _selectedDate, value))
                {
                    _ = LoadReportAsync();
                }
            }
        }

        public DailyReportDto Report
        {
            get => _report;
            set => SetProperty(ref _report, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand RefreshCommand { get; }

        public ReportsViewModel(ISaleRepository saleRepository)
        {
            _saleRepository = saleRepository;
            RefreshCommand = new RelayCommand(async () => await LoadReportAsync());
            _ = LoadReportAsync();
        }

        public async Task LoadReportAsync()
        {
            IsLoading = true;
            try
            {
                Report = await _saleRepository.GetDailyReportAsync(SelectedDate);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
