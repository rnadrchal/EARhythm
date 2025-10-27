using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Egami.Sequencer.UI.Converters;

public class BarOffsetConverter : IValueConverter
{
    // value: Wert zwischen -50 und +50
    // parameter: verfügbare Höhe (z.B. 50)
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        double val = System.Convert.ToDouble(value);
        double maxHeight = System.Convert.ToDouble(parameter);
        // 0 ist zentriert, positive Werte gehen nach oben, negative nach unten
        // Offset ist die Mitte minus die halbe Balkenhöhe
        double barHeight = Math.Abs(val) / 50.0 * (maxHeight / 2.0);
        if (val > 0)
            return new Thickness(0, (double)(maxHeight / 2.0) - barHeight, 0, 0);
        else
            return new Thickness(0,  (double)maxHeight / 2.0, 0, 0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}