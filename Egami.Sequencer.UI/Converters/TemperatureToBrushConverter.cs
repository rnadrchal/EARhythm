using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Egami.Sequencer.UI.Converters;

public class TemperatureToBrushConverter : IValueConverter
{
    // Konfiguration der Stops
    private const double ColdMin = -20.0; // -20 -> dunkelblau
    private const double HotMax = 35.0;   // 35 -> dunkelrot

    private static readonly Color DarkBlue = Color.FromRgb(0, 32, 96);   // dunkelblau
    private static readonly Color DarkRed = Color.FromRgb(139, 0, 0);    // dunkelrot (#8B0000)
    private static readonly Color White = Colors.White;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (!TryGetDouble(value, culture, out var temp))
            return new SolidColorBrush(White) { Opacity = 1.0 }; // Fallback

        Color col;
        if (temp <= 0.0)
        {
            if (temp <= ColdMin)
            {
                col = DarkBlue;
            }
            else
            {
                var t = (temp - ColdMin) / (0.0 - ColdMin); // 0..1
                col = Interpolate(DarkBlue, White, t);
            }
        }
        else
        {
            if (temp >= HotMax)
            {
                col = DarkRed;
            }
            else
            {
                var t = temp / HotMax; // 0..1
                col = Interpolate(White, DarkRed, t);
            }
        }

        var brush = new SolidColorBrush(col) { Opacity = 1.0 };
        brush.Freeze();
        return brush;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

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
            // Versuch einer allgemeinen Konvertierung
            result = System.Convert.ToDouble(value, culture ?? CultureInfo.InvariantCulture);
            return true;
        }
        catch
        {
            return false;
        }
    }

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