using System.Globalization;
using System.Windows.Data;

namespace Egami.Sequencer.UI.Converters;

public class EnumToOpacityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter == null || value == null)
            return 0.0;
        return value.ToString() == parameter.ToString() ? 1.0 : 0.2;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}