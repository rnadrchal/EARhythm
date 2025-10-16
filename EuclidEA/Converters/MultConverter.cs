using System;
using System.Globalization;
using System.Windows.Data;

namespace EuclidEA.Converters;

public class MultConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d && parameter is string sd && double.TryParse(sd, out double pd))
        {
            return d * pd;
        }

        if (value is int i && parameter is string s && int.TryParse(s, out int p))
        {
            return i * p;
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