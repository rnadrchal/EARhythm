using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Egami.Sequencer.UI.Converters;

public sealed class BoolToStrokeConverter : IValueConverter
{
    public Brush ActiveBrush { get; set; } = Brushes.OrangeRed;
    public Brush InactiveBrush { get; set; } = Brushes.Gray;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (value is bool b && b) ? ActiveBrush : InactiveBrush;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}