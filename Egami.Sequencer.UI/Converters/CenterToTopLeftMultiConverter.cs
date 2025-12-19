using System.Globalization;
using System.Windows.Data;

namespace Egami.Sequencer.UI.Converters;

// MultiValueConverter: (centerCoordinate, radius) => top-left coordinate
public class CenterToTopLeftMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values == null || values.Length < 2) return 0.0;
        if (!TryToDouble(values[0], out double center)) return 0.0;
        if (!TryToDouble(values[1], out double radius)) return center - radius;
        return center - radius;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private static bool TryToDouble(object o, out double d)
    {
        if (o is double db) { d = db; return true; }
        if (o is float f) { d = f; return true; }
        if (o is int i) { d = i; return true; }
        if (o is long l) { d = l; return true; }
        if (double.TryParse(o?.ToString(), out d)) return true;
        d = 0; return false;
    }
}