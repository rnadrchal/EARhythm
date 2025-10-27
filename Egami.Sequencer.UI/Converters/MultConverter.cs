using System.Globalization;
using System.Windows.Data;

namespace Egami.Sequencer.UI.Converters;

public class MultConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is string s && double.TryParse(s, CultureInfo.InvariantCulture, out double d))
        {
            if (value is byte b)
            {
                return b * d;
            }

            if (value is int i)
            {
                return i * d;
            }

            if (value is double dd)
            {
                return dd * d;
            }
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d && parameter is string sd && double.TryParse(sd, out double pd) && pd != 0)
        {
            return d / pd;
        }

        if (value is int i && parameter is string s && int.TryParse(s, out int p) && p != 0)
        {
            return i * p;
        }
        return value;
    }
}