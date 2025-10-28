using System.Globalization;
using System.Windows.Data;

namespace Egami.Sequencer.UI.Converters;

public class EnumToDoubleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => System.Convert.ToDouble(value);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (!targetType.IsEnum)
            throw new ArgumentException("Target type must be an enum.");

        if (value is double doubleValue)
            value = System.Convert.ToInt32(doubleValue); // Fully qualify Convert to avoid ambiguity

        return Enum.ToObject(targetType, value);
    }
}