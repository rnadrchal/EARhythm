using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Egami.Sequencer.UI.Converters;

public class OnOffToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var defaultBrush = "#222";
        if (parameter != null)
        {
            defaultBrush = parameter.ToString();
        }
        if (value is bool a)
        {
            return a ? "Red" : defaultBrush;
        }

        return Brushes.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}