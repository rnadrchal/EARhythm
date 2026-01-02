using Microsoft.Win32;
using Prism.Mvvm;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Syncfusion.Windows.Shared;

namespace Blattwerk.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly ImageConfiguration _imageConfig = new ImageConfiguration();
        public ImageConfiguration ImageConfig => _imageConfig;

        private string _title = "BLATTWERK";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        public ICommand LoadImageCommand { get; }

        public MainWindowViewModel()
        {
            LoadImageCommand = new DelegateCommand(_ => LoadImage());
        }

        private async Task LoadImage()
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter =
                    "Images (*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tiff)|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tiff|All Files (*.*)|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                await _imageConfig.LoadImageAsync(dlg.FileName);
            }
        }

    }
}
