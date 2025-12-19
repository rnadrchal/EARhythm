using System.Globalization;
using System.Windows.Data;

namespace Egami.Sequencer.UI.Converters;

public sealed class DoubleToDiameterConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d) return d * 2.0;
        if (value is float f) return (double)f * 2.0;
        if (value is int i) return (double)i * 2.0;
        return 0.0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}