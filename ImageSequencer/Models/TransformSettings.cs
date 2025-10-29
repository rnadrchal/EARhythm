using System;
using System.Linq;
using System.Windows.Media.Imaging;
using Egami.Imaging.Extensions;
using Egami.Imaging.Visiting;
using Prism.Mvvm;
using Syncfusion.Windows.Tools.Controls;

namespace ImageSequencer.Models;

public sealed class TransformSettings : BindableBase
{
    private readonly ApplicationSettings _applicationSettings;
    private static int[] BitDephts = new[]
    {
        1, 2, 4, 8, 24, 32, 48, 64
    };

    private double _scale = 1.0;
    public double Scale
    {
        get => _scale;
        set
        {
            if (SetProperty(ref _scale, value))
            {
                if (_applicationSettings?.Original != null)
                {
                    ScaleAndAdjustColorModel();
                    RaisePropertyChanged(nameof(BitmapSize));
                }
            }

        }
    }

    private ColorModel _colorModel = ColorModel.Original;
    public ColorModel ColorModel
    {
        get => _colorModel;
        set
        {
            if (SetProperty(ref _colorModel, value))
            {
                ScaleAndAdjustColorModel();
            }
        }
    }

    public string BitmapSize => $"{_applicationSettings?.Bitmap?.Width:N0} x {_applicationSettings?.Bitmap?.Height:N0}";

    public int MaxFormat => CropHelper.Formats.Length - 1;
    private int _formatIndex = 0;

    public string Format => CropHelper.Formats[_formatIndex];

    public int FormatIndex
    {
        get => _formatIndex;
        set
        {
            if (SetProperty(ref _formatIndex, value))
            {
                RaisePropertyChanged(nameof(Format));
            }
        }
    }

    private int _x;

    public int X
    {
        get => _x;
        set
        {
            SetProperty(ref _x, value);
        }
    }

    public int _y;

    public int Y
    {
        get => _y;
        set
        {
            SetProperty(ref _y, value);
        }
    }


    public int? MaxX => (int?)(_applicationSettings?.Bitmap?.Width - 1);
    public int? MaxY => (int?)(_applicationSettings?.Bitmap?.Height - 1);
    public TransformSettings(ApplicationSettings applicationSettings)
    {
        if (applicationSettings == null && applicationSettings.Original == null)
        {
            throw new ArgumentNullException(nameof(applicationSettings));
        }

        _scale = 1;
        _applicationSettings = applicationSettings;
        var ratio = CropHelper.GetAspectRatioString(applicationSettings.Original.PixelWidth, applicationSettings.Original.PixelHeight);
        var index = CropHelper.Formats.ToList().IndexOf(ratio);
        if (index < 0)
        {
            index = 0;
        }
        _formatIndex = index;
        _x = (int)(applicationSettings.Original.Width / 2);
        _y = (int)(applicationSettings.Original.Height / 2);
    }

    private void ScaleAndAdjustColorModel()
    {
        if (_applicationSettings?.Original != null)
        {
            var scaledBitmap = _applicationSettings.Original.ScaleBitmap(Scale);
            _applicationSettings.Bitmap = _colorModel switch
            {
                ColorModel.Grayscale => scaledBitmap.ToGrayscale(),
                ColorModel.Monochrome => scaledBitmap.ToMonochrome(),
                _ => scaledBitmap,
            };
            _applicationSettings.RenderTarget = new WriteableBitmap(_applicationSettings.Bitmap.PixelWidth, _applicationSettings.Bitmap.PixelHeight, _applicationSettings.Bitmap.DpiX, _applicationSettings.Bitmap.DpiY, _applicationSettings.Bitmap.Format, null);
            _applicationSettings.RequestReset();
        }

    }

}