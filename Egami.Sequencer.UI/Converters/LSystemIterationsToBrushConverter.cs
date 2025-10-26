using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Egami.Sequencer.UI.Converters;

public class LSystemIterationsToBrushConverter : IValueConverter
{
    private static SolidColorBrush _gray = new SolidColorBrush(Color.FromArgb(200, 64, 64, 64));
    private static SolidColorBrush _green = new SolidColorBrush(Color.FromArgb(200, 0, 64, 0));
    private static SolidColorBrush _red = new SolidColorBrush(Color.FromArgb(200, 64, 0, 0));
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int i)
        {
            return i switch
            {
                < 3 => _gray,
                < 6 => _green,
                _ => _red
            };

        }

        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}