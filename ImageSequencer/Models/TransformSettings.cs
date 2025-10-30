using System;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Egami.Imaging.Extensions;
using Egami.Imaging.Visiting;
using Prism.Mvvm;
using Syncfusion.Windows.Tools.Controls;

namespace ImageSequencer.Models;

public sealed class TransformSettings : BindableBase
{
    private readonly ApplicationSettings _applicationSettings;
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

    private Stretch _stretch = Stretch.Uniform;
    public Stretch Stretch
    {
        get => _stretch;
        set => SetProperty(ref _stretch, value);
    }

    public string BitmapSize => $"{(_applicationSettings?.Bitmap?.Width ?? 0):0} x {(_applicationSettings?.Bitmap?.Height ?? 0):0}";

    public TransformSettings(ApplicationSettings applicationSettings)
    {
        if (applicationSettings == null && applicationSettings.Original == null)
        {
            throw new ArgumentNullException(nameof(applicationSettings));
        }

        _scale = 1;
        _applicationSettings = applicationSettings;
    }

    public void Apply()
    {
        ScaleAndAdjustColorModel();
        RaisePropertyChanged(nameof(BitmapSize));
    }

    private void ScaleAndAdjustColorModel()
    {
        if (_applicationSettings?.Original != null)
        {
            var scaledBitmap = _applicationSettings.Original.ScaleBitmap(Scale);
            _applicationSettings.Bitmap = _colorModel switch
            {
                ColorModel.Grayscale => scaledBitmap.ToGrayscale(),
                _ => scaledBitmap,
            };
            _applicationSettings.RenderTarget = new WriteableBitmap(_applicationSettings.Bitmap.PixelWidth, _applicationSettings.Bitmap.PixelHeight, _applicationSettings.Bitmap.DpiX, _applicationSettings.Bitmap.DpiY, _applicationSettings.Bitmap.Format, null);
            _applicationSettings.RequestReset();
        }

    }

}