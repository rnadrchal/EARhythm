using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Windows;
using Prism.Mvvm;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Windows.Media.Imaging;
using Prism.Commands;
using Syncfusion.UI.Xaml.Charts;

namespace Blattwerk.ViewModels;

public enum ImageDirection
{
    [Display(Name="LTR")]
    Ltr,
    [Display(Name = "RTL")]
    Rtl,
    [Display(Name = "Top down")]
    TopDown,
    [Display(Name = "Bottom up")]
    BottomUp
}

public class ImageConfiguration : BindableBase, IDisposable
{
    private Image<Rgba32>? _image;

    private BitmapSource? _bitmap;

    private ImageDirection _direction = ImageDirection.Ltr;

    private int _columnOrRow = 0;
    public int ColumnOrRow
    {
        get => _columnOrRow;
        set => SetProperty(ref _columnOrRow, value);
    }

    private int _segmentCount = 32;
    public int SegmentCount
    {
        get => _segmentCount;
        set => SetProperty(ref _segmentCount, value);
    }

    public int[] SegmentCountes => [8, 16, 32, 64, 128];

    private int _segmentCountIndex = 2;

    public int SegmentCountIndex
    {
        get => _segmentCountIndex;
        set
        {
            if (SetProperty(ref _segmentCountIndex, value))
            {
                SegmentCount = SegmentCountes[value];
            }
        }
    }

    private int _maxPosition = 0;

    public int MaxPosition
    {
        get => _maxPosition;
        set => SetProperty(ref _maxPosition, value);
    }

    public DelegateCommand<ImageDirection?> SetDirectionCommand { get; }
    public ImageDirection Direction
    {
        get => _direction;
        set
        {
            if (SetProperty(ref _direction, value))
            {
                switch (value)
                {
                    case ImageDirection.Rtl:
                    case ImageDirection.Ltr:
                        MaxPosition = (int)(_bitmap.Height - 1);
                        break;
                    case ImageDirection.TopDown:
                    case ImageDirection.BottomUp:
                        MaxPosition = (int)(_bitmap.Width - 1);
                        break;
                }

                if (ColumnOrRow >= _maxPosition) ColumnOrRow = _maxPosition;
            }
        }
    }

    public BitmapSource? Bitmap
    {
        get => _bitmap;
        private set => SetProperty(ref _bitmap, value);
    }

    public ImageConfiguration()
    {
        SetDirectionCommand = new DelegateCommand<ImageDirection?>(d => Direction = d.HasValue ? d.Value : ImageDirection.Ltr);
    }


    // Asynchrone Variante (empfohlen für UI Responsiveness)
    public async System.Threading.Tasks.Task LoadImageAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return;

        _image?.Dispose();
        Bitmap = null;

        try
        {
            using var fs = File.OpenRead(path);
            var img = await Image.LoadAsync<Rgba32>(fs).ConfigureAwait(false);

            // Übergabe auf UI‑Thread nötig für SetProperty / BitmapSource
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                _image = img;
                Bitmap = ToBitmapSource(img);
                Bitmap?.Freeze();
                ColumnOrRow = 0;
                MaxPosition = _image.Width - 1;
                Direction = ImageDirection.Ltr;
            });
        }
        catch (Exception ex)
        {
            _image = null;
            Bitmap = null;
            MessageBox.Show(Application.Current.MainWindow, ex.Message, "Error loading Image", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }


    private static BitmapSource ToBitmapSource(Image<Rgba32> img)
    {
        // Verwende die Frame‑API und ProcessPixelRows / GetRowSpan, statt direkter GetPixelRowSpan-Aufrufe.
        var frame = img.Frames.RootFrame; // oder img.Frames[0]
        int w = frame.Width;
        int h = frame.Height;
        int stride = w * 4;
        var pixels = new byte[h * stride];

        // ProcessPixelRows liefert einen Row-Accessor; GetRowSpan(y) liefert die Pixel der Zeile.
        frame.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < h; y++)
            {
                var row = accessor.GetRowSpan(y); // Span<Rgba32>
                int rowOffset = y * stride;
                for (int x = 0; x < w; x++)
                {
                    var p = row[x];
                    pixels[rowOffset + x * 4 + 0] = p.B;
                    pixels[rowOffset + x * 4 + 1] = p.G;
                    pixels[rowOffset + x * 4 + 2] = p.R;
                    pixels[rowOffset + x * 4 + 3] = p.A;
                }
            }
        });

        var wb = new WriteableBitmap(w, h, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);
        wb.WritePixels(new System.Windows.Int32Rect(0, 0, w, h), pixels, stride, 0);
        wb.Freeze();
        return wb;
    }

    public void Dispose()
    {
        _image?.Dispose();
        _image = null;
        Bitmap = null;
    }
}