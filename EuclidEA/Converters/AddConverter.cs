using System;
using System.Globalization;
using System.Windows.Data;

namespace EuclidEA.Converters;

public class AddConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int i && parameter is string s && int.TryParse(s, out int p))
        {
            return i + p;
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}