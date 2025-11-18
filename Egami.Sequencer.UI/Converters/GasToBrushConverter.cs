using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Egami.Sequencer.UI.Converters;

public class GasToBrushConverter : IValueConverter
{
    // Defaults
    private const double DefaultMin = 10;
    private const double DefaultMax = 1000.0;  

    private static readonly Color DeepGreen = Color.FromRgb(0, 128, 64);
    private static readonly Color DeepRed = Color.FromRgb(192, 0, 0);
    private static readonly Color White = Colors.White;
    private static readonly Color FallbackGrey = Color.FromRgb(160, 160, 160);

    // Very simple, pre-frozen brushes for minimal allocations
    private static readonly SolidColorBrush GoodBrush = CreateFrozenBrush(DeepGreen);
    private static readonly SolidColorBrush NeutralBrush = CreateFrozenBrush(White);
    private static readonly SolidColorBrush BadBrush = CreateFrozenBrush(DeepRed);
    private static readonly SolidColorBrush FallbackBrush = CreateFrozenBrush(FallbackGrey);

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (!TryGetNumeric(value, culture, out var v))
            return FallbackBrush;

        // Extremely simple thresholding: use midpoint of defaults as boundary
        var min = DefaultMin;
        var max = DefaultMax;
        var mid = (min + max) * 0.5;

        if (double.IsNaN(v) || double.IsInfinity(v))
            return FallbackBrush;

        if (v >= mid) return GoodBrush;
        if (v <= min) return BadBrush;
        return NeutralBrush;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();

    private static bool TryGetNumeric(object? value, CultureInfo? culture, out double result)
    {
        result = double.NaN;
        if (value == null) return false;

        // Common numeric types
        switch (value)
        {
            case double d: result = d; return true;
            case float f: result = f; return true;
            case int i: result = i; return true;
            case long l: result = l; return true;
            case decimal m: result = (double)m; return true;
            case short s: result = s; return true;
            case byte b: result = b; return true;
            case string str:
                if (string.IsNullOrWhiteSpace(str)) return false;
                return double.TryParse(str, NumberStyles.Float | NumberStyles.AllowThousands, culture ?? CultureInfo.InvariantCulture, out result);
            default:
                try
                {
                    result = System.Convert.ToDouble(value, culture ?? CultureInfo.InvariantCulture);
                    return true;
                }
                catch
                {
                    return false;
                }
        }
    }

    private static SolidColorBrush CreateFrozenBrush(Color color)
    {
        var b = new SolidColorBrush(color);
        if (b.CanFreeze) b.Freeze();
        return b;
    }
}