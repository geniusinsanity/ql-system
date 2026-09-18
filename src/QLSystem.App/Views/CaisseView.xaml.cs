using System.Windows.Controls;
using System.Windows.Input;
using QLSystem.App.ViewModels;

namespace QLSystem.App.Views
{
    public partial class CaisseView : UserControl
    {
        public CaisseView()
        {
            InitializeComponent();
            Loaded += (s, e) => SearchInput.Focus();
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            if (DataContext is CaisseViewModel vm)
            {
                if (e.Key == Key.F1)
                {
                    SearchInput.Focus();
                    SearchInput.SelectAll();
                    e.Handled = true;
                }
                else if (e.Key == Key.F2)
                {
                    if (vm.CheckoutCashCommand.CanExecute(null))
                    {
                        vm.CheckoutCashCommand.Execute(null);
                    }
                    e.Handled = true;
                }
                else if (e.Key == Key.F3)
                {
                    if (vm.CheckoutCreditCommand.CanExecute(null))
                    {
                        vm.CheckoutCreditCommand.Execute(null);
                    }
                    e.Handled = true;
                }
                else if (e.Key == Key.F5)
                {
                    if (vm.ClearCartCommand.CanExecute(null))
                    {
                        vm.ClearCartCommand.Execute(null);
                    }
                    e.Handled = true;
                }
            }
        }
    }
}
