using System.Globalization;
using System.Windows.Data;

namespace Egami.Sequencer.UI.Converters;

public class BooleanToOpacityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter == null || !double.TryParse(parameter.ToString(), out var offValue))
        {
            offValue = 0.1;
        }
        if (value is bool b)
        {
            return b ? 1.0 : offValue;
        }

        return 0.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}