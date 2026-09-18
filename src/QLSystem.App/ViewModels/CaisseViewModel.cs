using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using QLSystem.App.Helpers;
using QLSystem.Core.DTOs;
using QLSystem.Core.Enums;
using QLSystem.Core.Interfaces;
using QLSystem.Core.Models;

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
        private ObservableCollection<Product> _searchResults = new();
        private Product? _selectedSearchProduct;
        private ObservableCollection<CartItemDto> _cartItems = new();
        private bool _isWholesaleMode = false;
        private bool _isSearchDropdownVisible = false;

        private decimal _discount;
        private decimal _paidAmount;
        private Customer? _selectedCustomer;
        private ObservableCollection<Customer> _customers = new();
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

        public bool IsWholesaleMode
        {
            get => _isWholesaleMode;
            set
            {
                if (SetProperty(ref _isWholesaleMode, value))
                {
                    // Update all cart items when toggling global price mode
                    foreach (var item in CartItems)
                    {
                        item.IsWholesale = value && item.WholesalePrice > 0;
                        item.UnitPrice = item.IsWholesale ? item.WholesalePrice : item.RetailPrice;
                    }
                    RefreshCartTotals();
                }
            }
        }

        public bool IsSearchDropdownVisible
        {
            get => _isSearchDropdownVisible;
            set => SetProperty(ref _isSearchDropdownVisible, value);
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

        public decimal ChangeAmount => PaidAmount > TotalAmount ? PaidAmount - TotalAmount : 0;
        public decimal DebtAmount => PaidAmount < TotalAmount ? TotalAmount - PaidAmount : 0;

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

        // Commands
        public ICommand AddToCartCommand { get; }
        public ICommand AddProductToCartCommand { get; }
        public ICommand RemoveFromCartCommand { get; }
        public ICommand IncreaseQuantityCommand { get; }
        public ICommand DecreaseQuantityCommand { get; }
        public ICommand CheckoutCashCommand { get; }
        public ICommand CheckoutCreditCommand { get; }
        public ICommand ClearCartCommand { get; }
        public ICommand SetRetailModeCommand { get; }
        public ICommand SetWholesaleModeCommand { get; }
        public ICommand TogglePriceModeCommand { get; }
        public ICommand ToggleItemPriceModeCommand { get; }
        public ICommand FocusSearchCommand { get; }

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
            AddProductToCartCommand = new RelayCommand(p => { if (p is Product prod) AddProductToCart(prod); });
            RemoveFromCartCommand = new RelayCommand(p => ExecuteRemoveFromCart(p as CartItemDto));
            IncreaseQuantityCommand = new RelayCommand(p => ExecuteChangeQuantity(p as CartItemDto, 1));
            DecreaseQuantityCommand = new RelayCommand(p => ExecuteChangeQuantity(p as CartItemDto, -1));
            CheckoutCashCommand = new RelayCommand(async () => await ExecuteCheckoutAsync(PaymentType.Cash));
            CheckoutCreditCommand = new RelayCommand(async () => await ExecuteCheckoutAsync(PaymentType.Credit));
            ClearCartCommand = new RelayCommand(ExecuteClearCart);
            SetRetailModeCommand = new RelayCommand(() => IsWholesaleMode = false);
            SetWholesaleModeCommand = new RelayCommand(() => IsWholesaleMode = true);
            TogglePriceModeCommand = new RelayCommand(() => IsWholesaleMode = !IsWholesaleMode);
            ToggleItemPriceModeCommand = new RelayCommand(p => ExecuteToggleItemPriceMode(p as CartItemDto));
            FocusSearchCommand = new RelayCommand(p =>
            {
                if (p is System.Windows.Controls.TextBox tb)
                {
                    tb.Focus();
                    tb.SelectAll();
                }
            });

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
                IsSearchDropdownVisible = false;
                return;
            }

            // Check for exact barcode scan
            var exactBarcodeProduct = await _productRepository.GetByBarcodeAsync(SearchQuery);
            if (exactBarcodeProduct != null)
            {
                AddProductToCart(exactBarcodeProduct);
                SearchQuery = string.Empty;
                IsSearchDropdownVisible = false;
                return;
            }

            var results = await _productRepository.SearchAsync(SearchQuery, limit: 12);
            SearchResults = new ObservableCollection<Product>(results);
            IsSearchDropdownVisible = SearchResults.Count > 0;
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
                bool useWholesale = IsWholesaleMode && p.WholesalePrice > 0;
                CartItems.Add(new CartItemDto
                {
                    ProductId = p.Id,
                    Reference = p.Reference,
                    Name = p.Name,
                    Dimensions = p.Dimensions ?? string.Empty,
                    RetailPrice = p.SalePrice,
                    WholesalePrice = p.WholesalePrice,
                    UnitPrice = useWholesale ? p.WholesalePrice : p.SalePrice,
                    PurchasePrice = p.PurchasePrice,
                    Quantity = 1,
                    Unit = p.SaleUnit,
                    AvailableStock = p.StockQuantity,
                    IsWholesale = useWholesale
                });
            }

            IsSearchDropdownVisible = false;
            RefreshCartTotals();
        }

        private void ExecuteAddToCart()
        {
            if (SelectedSearchProduct != null)
            {
                AddProductToCart(SelectedSearchProduct);
                SearchQuery = string.Empty;
                SearchResults.Clear();
                IsSearchDropdownVisible = false;
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

        private void ExecuteToggleItemPriceMode(CartItemDto? item)
        {
            if (item == null || item.WholesalePrice <= 0) return;
            item.IsWholesale = !item.IsWholesale;
            item.UnitPrice = item.IsWholesale ? item.WholesalePrice : item.RetailPrice;
            RefreshCartTotals();
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
                StatusMessage = "⚠ السلة فارغة! أضف سلعاً للمتابعة.";
                return;
            }

            if (paymentType == PaymentType.Credit && SelectedCustomer == null)
            {
                StatusMessage = "⚠ يرجى اختيار الزبون من القائمة لتسجيل البيع بالكريدي!";
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

            StatusMessage = $"✅ تم حفظ العملية بنجاح! الوصل: {sale.InvoiceNumber}";
            ExecuteClearCart();
            InvoiceNumber = await _saleRepository.GenerateNextInvoiceNumberAsync();
        }

        private void ExecuteClearCart()
        {
            CartItems.Clear();
            Discount = 0;
            PaidAmount = 0;
            SelectedCustomer = null;
            IsWholesaleMode = false;
            IsSearchDropdownVisible = false;
            RefreshCartTotals();
        }
    }
}
