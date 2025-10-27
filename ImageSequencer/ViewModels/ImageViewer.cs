using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;

namespace ImageSequencer.ViewModels;

public class ImageViewer : BindableBase
{
    private WriteableBitmap? _bitmap;
    public WriteableBitmap? Bitmap
    {
        get => _bitmap;
        set => SetProperty(ref _bitmap, value);
    }
    public ICommand OpenBitmapCommand { get; }
    public ICommand DropCommand { get; }

    public ImageViewer()
    {
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
            Bitmap = new WriteableBitmap(new BitmapImage(new Uri(dlg.FileName)));
        }
    }

    private void DropImage(DragEventArgs e)
    {

    }
}