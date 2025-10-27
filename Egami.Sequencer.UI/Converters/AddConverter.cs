using System.Globalization;
using System.Windows.Data;

namespace Egami.Sequencer.UI.Converters;

public class AddConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return value;

        if (double.TryParse(value.ToString(), out double v) &&
            double.TryParse(parameter.ToString(), out double p))
        {
            var result = v + p;
            // Optional: Rückgabe im Zieltyp
            if (targetType == typeof(int)) return (int)result;
            if (targetType == typeof(byte)) return (byte)result;
            if (targetType == typeof(double)) return result;
            return result;
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}