using System.Globalization;
using System.Windows.Data;

namespace Egami.Sequencer.UI.Converters;

public class ScaleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d)
        {
            double scale = parameter == null ? 100.0 : double.Parse(parameter.ToString()!);
            return d * scale;
        }

        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d)
        {
            double scale = parameter == null ? 100.0 : double.Parse(parameter.ToString()!);
            return d / scale;
        }

        return value;
    }
}