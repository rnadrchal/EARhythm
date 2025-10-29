using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ImageSequencer.Views
{
    /// <summary>
    /// Interaktionslogik für OutsourceWindow.xaml
    /// </summary>
    public partial class OutsourceWindow : Window
    {
        public OutsourceWindow()
        {
            InitializeComponent();
        }


        [DllImport("user32.dll")]
        static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);
        [DllImport("user32.dll")]
        static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);

        const uint MF_GRAYED = 0x00000001;
        const uint MF_ENABLED = 0x00000000;
        const uint SC_CLOSE = 0xF060;
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            IntPtr hMenu = GetSystemMenu(hwnd, false);
            EnableMenuItem(hMenu, SC_CLOSE, MF_GRAYED);
        }

    }
}
