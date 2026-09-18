using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using QLSystem.App.Helpers;
using QLSystem.Core.DTOs;
using QLSystem.Core.Enums;
using QLSystem.Core.Interfaces;
using QLSystem.Core.Models;
using QLSystem.Hardware;

namespace QLSystem.App.ViewModels
{
    /// <summary>
    /// إدارة شاشة الكاسة والبيع السريع للكانكري (Caisse / Comptoir)
    /// </summary>
    public class CaisseViewModel : ObservableObject
    {
        private readonly IProductRepository _productRepository;
        private readonly ISaleRepository _saleRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly ISettingsRepository _settingsRepository;

        private string _searchQuery = string.Empty;
        private ObservableCollection<Product> _searchResults = new ObservableCollection<Product>();
        private Product? _selectedSearchProduct;
        private ObservableCollection<CartItemDto> _cartItems = new ObservableCollection<CartItemDto>();

        private decimal _discount;
        private decimal _paidAmount;
        private Customer? _selectedCustomer;
        private ObservableCollection<Customer> _customers = new ObservableCollection<Customer>();
        private string _invoiceNumber = string.Empty;
        private string _statusMessage = string.Empty;

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (SetProperty(ref _searchQuery, value))
                {
                    _ = PerformSearchAsync();
                }
            }
        }

        public ObservableCollection<Product> SearchResults
        {
            get => _searchResults;
            set => SetProperty(ref _searchResults, value);
        }

        public Product? SelectedSearchProduct
        {
            get => _selectedSearchProduct;
            set => SetProperty(ref _selectedSearchProduct, value);
        }

        public ObservableCollection<CartItemDto> CartItems
        {
            get => _cartItems;
            set => SetProperty(ref _cartItems, value);
        }

        public decimal SubTotal => CartItems.Sum(x => x.TotalPrice);

        public decimal Discount
        {
            get => _discount;
            set
            {
                if (SetProperty(ref _discount, value))
                {
                    OnPropertyChanged(nameof(TotalAmount));
                    OnPropertyChanged(nameof(ChangeAmount));
                }
            }
        }

        public decimal TotalAmount
        {
            get
            {
                var total = SubTotal - Discount;
                return total < 0 ? 0 : total;
            }
        }

        public decimal PaidAmount
        {
            get => _paidAmount;
            set
            {
                if (SetProperty(ref _paidAmount, value))
                {
                    OnPropertyChanged(nameof(ChangeAmount));
                    OnPropertyChanged(nameof(DebtAmount));
                }
            }
        }

        public decimal ChangeAmount
        {
            get
            {
                if (PaidAmount > TotalAmount) return PaidAmount - TotalAmount;
                return 0;
            }
        }

        public decimal DebtAmount
        {
            get
            {
                if (PaidAmount < TotalAmount) return TotalAmount - PaidAmount;
                return 0;
            }
        }

        public Customer? SelectedCustomer
        {
            get => _selectedCustomer;
            set => SetProperty(ref _selectedCustomer, value);
        }

        public ObservableCollection<Customer> Customers
        {
            get => _customers;
            set => SetProperty(ref _customers, value);
        }

        public string InvoiceNumber
        {
            get => _invoiceNumber;
            set => SetProperty(ref _invoiceNumber, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ICommand AddToCartCommand { get; }
        public ICommand RemoveFromCartCommand { get; }
        public ICommand IncreaseQuantityCommand { get; }
        public ICommand DecreaseQuantityCommand { get; }
        public ICommand CheckoutCashCommand { get; }
        public ICommand CheckoutCreditCommand { get; }
        public ICommand ClearCartCommand { get; }

        public CaisseViewModel(
            IProductRepository productRepository,
            ISaleRepository saleRepository,
            ICustomerRepository customerRepository,
            ISettingsRepository settingsRepository)
        {
            _productRepository = productRepository;
            _saleRepository = saleRepository;
            _customerRepository = customerRepository;
            _settingsRepository = settingsRepository;

            AddToCartCommand = new RelayCommand(ExecuteAddToCart);
            RemoveFromCartCommand = new RelayCommand(p => ExecuteRemoveFromCart(p as CartItemDto));
            IncreaseQuantityCommand = new RelayCommand(p => ExecuteChangeQuantity(p as CartItemDto, 1));
            DecreaseQuantityCommand = new RelayCommand(p => ExecuteChangeQuantity(p as CartItemDto, -1));
            CheckoutCashCommand = new RelayCommand(async () => await ExecuteCheckoutAsync(PaymentType.Cash));
            CheckoutCreditCommand = new RelayCommand(async () => await ExecuteCheckoutAsync(PaymentType.Credit));
            ClearCartCommand = new RelayCommand(ExecuteClearCart);

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            InvoiceNumber = await _saleRepository.GenerateNextInvoiceNumberAsync();
            var custList = await _customerRepository.GetAllAsync();
            Customers = new ObservableCollection<Customer>(custList);
        }

        public async Task PerformSearchAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery))
            {
                SearchResults.Clear();
                return;
            }

            // فحص هل هو مسح باركود كامل
            var exactBarcodeProduct = await _productRepository.GetByBarcodeAsync(SearchQuery);
            if (exactBarcodeProduct != null)
            {
                AddProductToCart(exactBarcodeProduct);
                SearchQuery = string.Empty;
                return;
            }

            var results = await _productRepository.SearchAsync(SearchQuery, limit: 15);
            SearchResults = new ObservableCollection<Product>(results);
        }

        public void AddProductToCart(Product p)
        {
            var existing = CartItems.FirstOrDefault(x => x.ProductId == p.Id);
            if (existing != null)
            {
                existing.Quantity += 1;
            }
            else
            {
                bool useWholesale = SelectedCustomer?.AppliesWholesalePrice ?? false;
                CartItems.Add(new CartItemDto
                {
                    ProductId = p.Id,
                    Reference = p.Reference,
                    Name = p.Name,
                    UnitPrice = useWholesale ? p.WholesalePrice : p.SalePrice,
                    PurchasePrice = p.PurchasePrice,
                    Quantity = 1,
                    Unit = p.SaleUnit,
                    AvailableStock = p.StockQuantity,
                    IsWholesale = useWholesale
                });
            }

            RefreshCartTotals();
        }

        private void ExecuteAddToCart()
        {
            if (SelectedSearchProduct != null)
            {
                AddProductToCart(SelectedSearchProduct);
                SearchQuery = string.Empty;
                SearchResults.Clear();
            }
        }

        private void ExecuteRemoveFromCart(CartItemDto? item)
        {
            if (item != null)
            {
                CartItems.Remove(item);
                RefreshCartTotals();
            }
        }

        private void ExecuteChangeQuantity(CartItemDto? item, decimal delta)
        {
            if (item != null)
            {
                item.Quantity += delta;
                if (item.Quantity <= 0)
                {
                    CartItems.Remove(item);
                }
                RefreshCartTotals();
            }
        }

        private void RefreshCartTotals()
        {
            OnPropertyChanged(nameof(SubTotal));
            OnPropertyChanged(nameof(TotalAmount));
            PaidAmount = TotalAmount; // تعبئة تلقائية للكاش
            OnPropertyChanged(nameof(ChangeAmount));
            OnPropertyChanged(nameof(DebtAmount));
        }

        private async Task ExecuteCheckoutAsync(PaymentType paymentType)
        {
            if (!CartItems.Any())
            {
                StatusMessage = "السلة فارغة!";
                return;
            }

            if (paymentType == PaymentType.Credit && SelectedCustomer == null)
            {
                StatusMessage = "يرجى اختيار الزبون لتسجيل البيع بالكريدي!";
                return;
            }

            var sale = new Sale
            {
                InvoiceNumber = InvoiceNumber,
                CustomerId = SelectedCustomer?.Id,
                CustomerName = SelectedCustomer?.FullName ?? "زبون عادي (Comptoir)",
                PaymentType = paymentType,
                SubTotal = SubTotal,
                Discount = Discount,
                TotalAmount = TotalAmount,
                PaidAmount = paymentType == PaymentType.Cash ? PaidAmount : (PaidAmount > TotalAmount ? TotalAmount : PaidAmount),
                ChangeAmount = paymentType == PaymentType.Cash ? ChangeAmount : 0,
                DebtAmount = paymentType == PaymentType.Credit ? (TotalAmount - PaidAmount) : 0,
                ProfitAmount = CartItems.Sum(x => x.Profit),
                CreatedAt = DateTime.Now
            };

            foreach (var cartItem in CartItems)
            {
                sale.Items.Add(new SaleItem
                {
                    ProductId = cartItem.ProductId,
                    ProductReference = cartItem.Reference,
                    ProductName = cartItem.Name,
                    Quantity = cartItem.Quantity,
                    Unit = cartItem.Unit,
                    UnitPrice = cartItem.UnitPrice,
                    PurchasePrice = cartItem.PurchasePrice,
                    TotalPrice = cartItem.TotalPrice
                });
            }

            await _saleRepository.CreateSaleAsync(sale);

            StatusMessage = $"تم حفظ العملية بنجاح! الوصل: {sale.InvoiceNumber}";
            ExecuteClearCart();
            InvoiceNumber = await _saleRepository.GenerateNextInvoiceNumberAsync();
        }

        private void ExecuteClearCart()
        {
            CartItems.Clear();
            Discount = 0;
            PaidAmount = 0;
            SelectedCustomer = null;
            RefreshCartTotals();
        }
    }
}
