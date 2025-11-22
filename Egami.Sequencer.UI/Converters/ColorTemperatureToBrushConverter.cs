using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Egami.Sequencer.UI.Converters;

public class ColorTemperatureToBrushConverter : IValueConverter
{
    private static readonly Color High = Colors.Blue;
    private static readonly Color Low = Colors.OrangeRed;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double lux)
        {
            lux = Math.Clamp(lux, 0.0, 1.0);

            byte r = (byte)(High.R + (Low.R - High.R) * lux);
            byte g = (byte)(High.G + (Low.G - High.G) * lux);
            byte b = (byte)(High.B + (Low.B - High.B) * lux);

            return new SolidColorBrush(Color.FromRgb(r, g, b));
        }
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}