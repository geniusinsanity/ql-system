using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using QLSystem.App.Helpers;
using QLSystem.Core.Enums;
using QLSystem.Core.Interfaces;
using QLSystem.Core.Models;

namespace QLSystem.App.ViewModels
{
    /// <summary>
    /// إدارة ديون الزبائن والتسديدات (Crédits & Versements)
    /// </summary>
    public class CreditViewModel : ObservableObject
    {
        private readonly ICustomerRepository _customerRepository;

        private ObservableCollection<Customer> _customers = new ObservableCollection<Customer>();
        private Customer? _selectedCustomer;
        private ObservableCollection<CustomerPayment> _paymentHistory = new ObservableCollection<CustomerPayment>();
        private decimal _totalCredit;
        private decimal _paymentAmount;
        private string _paymentNotes = string.Empty;
        private string _statusMessage = string.Empty;

        public ObservableCollection<Customer> Customers
        {
            get => _customers;
            set => SetProperty(ref _customers, value);
        }

        public Customer? SelectedCustomer
        {
            get => _selectedCustomer;
            set
            {
                if (SetProperty(ref _selectedCustomer, value))
                {
                    _ = LoadPaymentHistoryAsync();
                }
            }
        }

        public ObservableCollection<CustomerPayment> PaymentHistory
        {
            get => _paymentHistory;
            set => SetProperty(ref _paymentHistory, value);
        }

        public decimal TotalCredit
        {
            get => _totalCredit;
            set => SetProperty(ref _totalCredit, value);
        }

        public decimal PaymentAmount
        {
            get => _paymentAmount;
            set => SetProperty(ref _paymentAmount, value);
        }

        public string PaymentNotes
        {
            get => _paymentNotes;
            set => SetProperty(ref _paymentNotes, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ICommand RecordPaymentCommand { get; }
        public ICommand RefreshCommand { get; }

        public CreditViewModel(ICustomerRepository customerRepository)
        {
            _customerRepository = customerRepository;

            RecordPaymentCommand = new RelayCommand(async () => await ExecuteRecordPaymentAsync());
            RefreshCommand = new RelayCommand(async () => await LoadCustomersAsync());

            _ = LoadCustomersAsync();
        }

        public async Task LoadCustomersAsync()
        {
            var list = await _customerRepository.GetAllAsync();
            Customers = new ObservableCollection<Customer>(list);
            TotalCredit = await _customerRepository.GetTotalCreditGivenAsync();
        }

        private async Task LoadPaymentHistoryAsync()
        {
            if (SelectedCustomer == null)
            {
                PaymentHistory.Clear();
                return;
            }

            var history = await _customerRepository.GetPaymentHistoryAsync(SelectedCustomer.Id);
            PaymentHistory = new ObservableCollection<CustomerPayment>(history);
        }

        private async Task ExecuteRecordPaymentAsync()
        {
            if (SelectedCustomer == null)
            {
                StatusMessage = "يرجى اختيار الزبون أولاً!";
                return;
            }

            if (PaymentAmount <= 0)
            {
                StatusMessage = "يرجى إدخال مبلغ تسديد صالح!";
                return;
            }

            var payment = new CustomerPayment
            {
                CustomerId = SelectedCustomer.Id,
                Amount = PaymentAmount,
                PaymentMethod = PaymentType.Cash,
                Notes = PaymentNotes,
                PaymentDate = DateTime.Now
            };

            var success = await _customerRepository.RecordPaymentAsync(payment);
            if (success)
            {
                StatusMessage = $"تم تسجيل دفعة {PaymentAmount:N2} دج بنجاح!";
                PaymentAmount = 0;
                PaymentNotes = string.Empty;
                await LoadCustomersAsync();
                await LoadPaymentHistoryAsync();
            }
        }
    }
}
