using System;
using System.Globalization;
using System.Windows.Data;
using Material.Icons;

namespace EuclidEA.Converters;

public class RunningToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isRunning)
        {
            return isRunning ? MaterialIconKind.Pause : MaterialIconKind.Play;
        }
        return MaterialIconKind.HelpCircleOutline;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}