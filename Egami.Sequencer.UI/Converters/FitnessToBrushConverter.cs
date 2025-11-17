using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Egami.Sequencer.UI.Converters;

public class FitnessToBrushConverter : IValueConverter
{
    // Dunkelrot: #8B0000 (139,0,0), Hellgrün: #90EE90 (144,238,144)
    private static readonly Color Yellow = Colors.Yellow;
    private static readonly Color DarkRed = Colors.DarkRed;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double fitness)
        {
            fitness = Math.Clamp(fitness, 0.0, 1.0);

            byte r = (byte)(Yellow.R + (DarkRed.R - Yellow.R) * fitness);
            byte g = (byte)(Yellow.G + (DarkRed.G - Yellow.G) * fitness);
            byte b = (byte)(Yellow.B + (DarkRed.B - Yellow.B) * fitness);

            return new SolidColorBrush(Color.FromRgb(r, g, b));
        }

        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}