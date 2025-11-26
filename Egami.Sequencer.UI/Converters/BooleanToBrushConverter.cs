using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Egami.Sequencer.UI.Converters;

public class BooleanToBrushConverter : IValueConverter
{
    private static SolidColorBrush _gray = new SolidColorBrush(Color.FromArgb(200, 64, 64, 64));
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            var color = (parameter != null 
                ? (Color?)ColorConverter.ConvertFromString(parameter.ToString())
                : null)
                ?? Colors.Red;
            return boolValue ? new SolidColorBrush(color) : _gray;
        }
        return Brushes.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}