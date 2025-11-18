using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Egami.Sequencer.UI.Converters;

public class PressureToBrushConverter : IValueConverter
{
    // Konfigurierbare Grenzen (in hPa)
    private const double LowMin = 950.0;                    // sehr tiefer Druck (extrem)
    private const double NormalCenter = 1013.25;            // "medizinisch ideal"
    private const double NormalHalfWidth = 10.0;            // +/- um Center => Peak-Breite
    private const double NormalMin = NormalCenter - NormalHalfWidth; // 1003.25
    private const double NormalMax = NormalCenter + NormalHalfWidth; // 1023.25
    private const double HighMax = 1085.0;                  // sehr hoher Druck (extrem)

    // Farben
    private static readonly Color DeepBlue = Color.FromRgb(0, 48, 128);   // tiefes Blau
    private static readonly Color DeepGreen = Color.FromRgb(0, 128, 64);  // tiefes Grün (Peak)
    private static readonly Color DeepRed = Color.FromRgb(192, 0, 0);     // gesättigtes Rot
    private static readonly Color White = Colors.White;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (!TryGetDouble(value, culture, out var pressure))
            return new SolidColorBrush(White) { Opacity = 1.0 }.FreezeAndReturn();

        Color col;

        if (pressure <= NormalMin)
        {
            // Niedrigbereich: DeepBlue -> Weiß (bei steigendem Druck)
            var t = Normalize(pressure, LowMin, NormalMin); // 0..1
            col = Interpolate(DeepBlue, White, t);
        }
        else if (pressure >= NormalMax)
        {
            // Hochbereich: Weiß -> DeepRed (bei steigendem Druck)
            var t = Normalize(pressure, NormalMax, HighMax); // 0..1
            col = Interpolate(White, DeepRed, t);
        }
        else
        {
            // Normalbereich: Peak bei NormalCenter mit DeepGreen,
            // abnehmen der Sättigung zu beiden Seiten -> Weiß am Rand
            var d = Math.Abs(pressure - NormalCenter) / NormalHalfWidth; // 0..1
            col = Interpolate(DeepGreen, White, d);
        }

        return new SolidColorBrush(col) { Opacity = 1.0 }.FreezeAndReturn();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    // Hilfsmethoden
    private static bool TryGetDouble(object? value, CultureInfo culture, out double result)
    {
        result = double.NaN;
        if (value == null) return false;
        if (value is double d) { result = d; return true; }
        if (value is float f) { result = f; return true; }
        if (value is int i) { result = i; return true; }
        if (value is long l) { result = l; return true; }
        if (value is string s && double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, culture ?? CultureInfo.InvariantCulture, out var p))
        {
            result = p;
            return true;
        }
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

    private static double Normalize(double v, double min, double max)
    {
        if (double.IsNaN(v) || double.IsInfinity(v)) return 0.0;
        if (max <= min) return v <= min ? 0.0 : 1.0;
        var t = (v - min) / (max - min);
        return Math.Clamp(t, 0.0, 1.0);
    }

    // Linear interpolation between colors (including alpha)
    private static Color Interpolate(Color a, Color b, double t)
    {
        t = Math.Clamp(t, 0.0, 1.0);
        byte A = (byte)Math.Round(a.A + (b.A - a.A) * t);
        byte R = (byte)Math.Round(a.R + (b.R - a.R) * t);
        byte G = (byte)Math.Round(a.G + (b.G - a.G) * t);
        byte B = (byte)Math.Round(a.B + (b.B - a.B) * t);
        return Color.FromArgb(A, R, G, B);
    }
}

// Extension für gefrorene Brushes (kleine Helferfunktion)
static class BrushExtensions
{
    public static SolidColorBrush FreezeAndReturn(this SolidColorBrush brush)
    {
        if (brush.CanFreeze)
        {
            brush.Freeze();
        }
        return brush;
    }
}