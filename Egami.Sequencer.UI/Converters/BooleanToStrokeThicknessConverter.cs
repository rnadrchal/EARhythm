using System.Globalization;
using System.Windows.Data;

namespace Egami.Sequencer.UI.Converters;

public sealed class BoolToStrokeThicknessConverter : IValueConverter
{
    public double ActiveThickness { get; set; } = 2.6;
    public double InactiveThickness { get; set; } = 1.0;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => (value is bool b && b) ? ActiveThickness : InactiveThickness;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}