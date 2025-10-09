using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace EuclidEA.Converters;

public class BooleanToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            var color = (parameter != null 
                ? (Color?)ColorConverter.ConvertFromString(parameter.ToString())
                : null)
                ?? Colors.Red;
            return boolValue ? new SolidColorBrush(color) : Brushes.DarkGray;
        }
        return Brushes.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}