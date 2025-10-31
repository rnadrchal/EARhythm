using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Egami.Sequencer.UI.Converters;

public class ActiveToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var defaultBrush = "Orange";
        if (parameter != null)
        {
            defaultBrush = parameter.ToString();
        }
        if (value is bool a)
        {
            return a ? "LightGreen" : defaultBrush;
        }

        return Brushes.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}