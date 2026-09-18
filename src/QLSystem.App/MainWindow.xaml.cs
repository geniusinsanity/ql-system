using System.Windows;
using QLSystem.App.ViewModels;

namespace QLSystem.App
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
