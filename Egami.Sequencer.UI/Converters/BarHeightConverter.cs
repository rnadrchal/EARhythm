using System.Globalization;
using System.Windows.Data;

namespace Egami.Sequencer.UI.Converters;

public class BarHeightConverter : IValueConverter
{
    // value: Wert zwischen -50 und +50
    // parameter: verfügbare Höhe (z.B. 50)
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        double val = System.Convert.ToDouble(value);
        double maxHeight = System.Convert.ToDouble(parameter);
        double absVal = Math.Abs(val);
        // Skaliere auf die Hälfte der Höhe (0 = 0, 50 = maxHeight/2)
        return absVal / 50.0 * (maxHeight / 2.0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}