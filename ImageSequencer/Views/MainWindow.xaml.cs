using System.ComponentModel;
using System.Windows;
using ImageSequencer.ViewModels;

namespace ImageSequencer.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.CloseOutsource();
            }
        }
    }
}
