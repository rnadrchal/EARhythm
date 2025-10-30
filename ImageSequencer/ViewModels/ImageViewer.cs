using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ImageSequencer.Models;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;

namespace ImageSequencer.ViewModels;

public class ImageViewer : BindableBase
{
    private readonly ApplicationSettings _applicationSettings;
    public ApplicationSettings ApplicationSettings => _applicationSettings;
    public ICommand OpenBitmapCommand { get; }
    public ICommand DropCommand { get; }

    public ImageViewer(ApplicationSettings applicationSettings)
    {
        _applicationSettings = applicationSettings;
        OpenBitmapCommand = new DelegateCommand(OpenBitmap);
        DropCommand = new DelegateCommand<DragEventArgs>(DropImage);
    }

    private void OpenBitmap()
    {
        OpenFileDialog dlg = new OpenFileDialog
        {
            Filter =
                "Images (*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tiff)|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tiff|All Files (*.*)|*.*"
        };
        if (dlg.ShowDialog() == true)
        {
            LoadBitmap(dlg.FileName);
        }
    }

    // Neu: Lädt ein Bild programmgesteuert (z. B. beim App-Start)
    public void LoadBitmap(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        try
        {
            _applicationSettings.FilePath = path;
            var wb = new WriteableBitmap(new BitmapImage(new Uri(path)));
            _applicationSettings.Bitmap = wb;
            _applicationSettings.Original = wb.Clone();
            _applicationSettings.RenderTarget = new WriteableBitmap(_applicationSettings.Bitmap.PixelWidth, _applicationSettings.Bitmap.PixelHeight, _applicationSettings.Bitmap.DpiX, _applicationSettings.Bitmap.DpiY, _applicationSettings.Bitmap.Format, null);
            _applicationSettings.TransformSettings ??= new TransformSettings(_applicationSettings);
            _applicationSettings.TransformSettings.Apply();
            _applicationSettings.RequestReset();
        }
        catch
        {
            // optional: Logging einbauen; hier still fail-safe
        }
    }

    private void DropImage(DragEventArgs e)
    {

    }
}