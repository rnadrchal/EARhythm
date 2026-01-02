using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Egami.Sequencer.UI.Converters;

public sealed class ByteArrayToImageSourceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not byte[] bytes || bytes.Length == 0)
            return null;

        try
        {
            using var ms = new MemoryStream(bytes);
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad; // vollständig in memory laden, damit Stream geschlossen werden kann
            bmp.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
            bmp.StreamSource = ms;
            bmp.EndInit();
            bmp.Freeze(); // für Thread-Safety / UI-Thread-Übertragung
            return bmp as ImageSource;
        }
        catch
        {
            return null;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}