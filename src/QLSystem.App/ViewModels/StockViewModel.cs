using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using QLSystem.App.Helpers;
using QLSystem.Core.Interfaces;
using QLSystem.Core.Models;
using QLSystem.Hardware;

namespace QLSystem.App.ViewModels
{
    /// <summary>
    /// إدارة المخزون وقائمة سلع الكانكري (Gestion de Stock)
    /// </summary>
    public class StockViewModel : ObservableObject
    {
        private readonly IProductRepository _productRepository;

        private ObservableCollection<Product> _products = new ObservableCollection<Product>();
        private Product? _selectedProduct;
        private string _searchFilter = string.Empty;
        private bool _onlyLowStock;
        private string _statusMessage = string.Empty;

        public ObservableCollection<Product> Products
        {
            get => _products;
            set => SetProperty(ref _products, value);
        }

        public Product? SelectedProduct
        {
            get => _selectedProduct;
            set => SetProperty(ref _selectedProduct, value);
        }

        public string SearchFilter
        {
            get => _searchFilter;
            set
            {
                if (SetProperty(ref _searchFilter, value))
                {
                    _ = LoadProductsAsync();
                }
            }
        }

        public bool OnlyLowStock
        {
            get => _onlyLowStock;
            set
            {
                if (SetProperty(ref _onlyLowStock, value))
                {
                    _ = LoadProductsAsync();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand SaveProductCommand { get; }
        public ICommand DeleteProductCommand { get; }
        public ICommand GenerateBarcodeCommand { get; }

        public StockViewModel(IProductRepository productRepository)
        {
            _productRepository = productRepository;

            RefreshCommand = new RelayCommand(async () => await LoadProductsAsync());
            SaveProductCommand = new RelayCommand(async () => await ExecuteSaveProductAsync());
            DeleteProductCommand = new RelayCommand(async () => await ExecuteDeleteProductAsync());
            GenerateBarcodeCommand = new RelayCommand(ExecuteGenerateBarcode);

            _ = LoadProductsAsync();
        }

        public async Task LoadProductsAsync()
        {
            if (OnlyLowStock)
            {
                var low = await _productRepository.GetLowStockProductsAsync();
                Products = new ObservableCollection<Product>(low);
            }
            else if (!string.IsNullOrWhiteSpace(SearchFilter))
            {
                var search = await _productRepository.SearchAsync(SearchFilter);
                Products = new ObservableCollection<Product>(search);
            }
            else
            {
                var all = await _productRepository.GetAllAsync();
                Products = new ObservableCollection<Product>(all);
            }
        }

        private async Task ExecuteSaveProductAsync()
        {
            if (SelectedProduct == null) return;

            if (string.IsNullOrWhiteSpace(SelectedProduct.Name) || string.IsNullOrWhiteSpace(SelectedProduct.Reference))
            {
                StatusMessage = "يرجى ملء اسم السلعة والمرجع!";
                return;
            }

            if (SelectedProduct.Id == 0)
            {
                await _productRepository.AddAsync(SelectedProduct);
                StatusMessage = "تمت إضافة السلعة الجديدة بنجاح!";
            }
            else
            {
                await _productRepository.UpdateAsync(SelectedProduct);
                StatusMessage = "تم تحديث السلعة بنجاح!";
            }

            await LoadProductsAsync();
        }

        private async Task ExecuteDeleteProductAsync()
        {
            if (SelectedProduct != null && SelectedProduct.Id > 0)
            {
                await _productRepository.DeleteAsync(SelectedProduct.Id);
                StatusMessage = "تم حذف السلعة بنجاح.";
                await LoadProductsAsync();
            }
        }

        private void ExecuteGenerateBarcode()
        {
            if (SelectedProduct != null)
            {
                var id = SelectedProduct.Id == 0 ? new Random().Next(1000, 99999) : SelectedProduct.Id;
                SelectedProduct.Barcode = BarcodeService.GenerateInternalBarcode(id);
                OnPropertyChanged(nameof(SelectedProduct));
                StatusMessage = $"تم توليد كود باركود داخلي: {SelectedProduct.Barcode}";
            }
        }
    }
}
