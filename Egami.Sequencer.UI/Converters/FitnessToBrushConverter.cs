using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Egami.Sequencer.UI.Converters;

public class FitnessToBrushConverter : IValueConverter
{
    // Dunkelrot: #8B0000 (139,0,0), Hellgrün: #90EE90 (144,238,144)
    private static readonly Color DarkRed = Color.FromRgb(139, 0, 0);
    private static readonly Color LightGreen = Color.FromRgb(144, 238, 144);

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double fitness)
        {
            fitness = Math.Clamp(fitness, 0.0, 1.0);

            byte r = (byte)(DarkRed.R + (LightGreen.R - DarkRed.R) * fitness);
            byte g = (byte)(DarkRed.G + (LightGreen.G - DarkRed.G) * fitness);
            byte b = (byte)(DarkRed.B + (LightGreen.B - DarkRed.B) * fitness);

            return new SolidColorBrush(Color.FromRgb(r, g, b));
        }

        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}