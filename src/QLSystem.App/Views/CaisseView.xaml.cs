using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using QLSystem.App.ViewModels;
using QLSystem.Core.DTOs;

namespace QLSystem.App.Views
{
    public partial class CaisseView : UserControl
    {
        public CaisseView()
        {
            InitializeComponent();
            SearchInput.Focus();
        }

        // Double-click on search result → add to cart
        private void SearchItem_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is CaisseViewModel vm && vm.SelectedSearchProduct != null)
            {
                vm.AddProductToCartCommand.Execute(vm.SelectedSearchProduct);
            }
        }

        // Click on price badge → toggle Gros/Détail for that item
        private void PriceBadge_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is CartItemDto item
                && DataContext is CaisseViewModel vm)
            {
                vm.ToggleItemPriceModeCommand.Execute(item);
            }
        }
    }
}
