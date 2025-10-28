using System.Globalization;
using System.Windows.Data;

namespace Egami.Sequencer.UI.Converters;

public class EnumToBooleanConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter == null || value == null)
            return false;
        return value.ToString() == parameter.ToString();

    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}