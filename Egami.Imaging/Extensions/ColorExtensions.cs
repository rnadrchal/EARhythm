using System.Windows.Media;

namespace Egami.Imaging.Extensions;

public static class ColorExtensions
{
    public static byte BaseColorSevenBit(this Color color, BaseColor baseColor)
    {
        return baseColor switch
        {
            BaseColor.Red => (byte)(color.R / 2),
            BaseColor.Green => (byte)(color.G / 2),
            BaseColor.Blue => (byte)(color.B / 2),
            BaseColor.Yellow => (byte)(((color.R + color.G) / 2) / 2), // Mittelwert aus Rot und Grün, dann auf 0-127 skalieren
            BaseColor.Cyan => (byte)(((color.G + color.B) / 2) / 2),
            BaseColor.Magenta => (byte)(((color.R + color.B) / 2) / 2),
            _ => throw new ArgumentOutOfRangeException(nameof(baseColor), baseColor, null)
        };
    }

    public static byte BrightnessSevenBit(this Color color)
    {
        var luminance = 0.299 * color.R + 0.587 * color.G + 0.114 * color.B;
        return (byte)(luminance / 2); // Skaliert auf 0-127
    }

    public static byte HueSevenBit(this Color color)
    {
        // RGB zu HSV-Konvertierung
        double r = color.R / 255.0;
        double g = color.G / 255.0;
        double b = color.B / 255.0;

        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        double delta = max - min;

        double hue = 0;
        if (delta != 0)
        {
            if (max == r)
                hue = 60 * (((g - b) / delta) % 6);
            else if (max == g)
                hue = 60 * (((b - r) / delta) + 2);
            else
                hue = 60 * (((r - g) / delta) + 4);
        }
        if (hue < 0) hue += 360;

        return (byte)(hue / 360.0 * 127); // Skaliert auf 0-127
    }

    public static byte SaturationSevenBit(this Color color)
    {
        double r = color.R / 255.0;
        double g = color.G / 255.0;
        double b = color.B / 255.0;

        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        double delta = max - min;

        double saturation = max == 0 ? 0 : delta / max;
        return (byte)(saturation * 127); // Skaliert auf 0-127
    }

    public static byte LightnessSevenBit(this Color color)
    {
        double r = color.R / 255.0;
        double g = color.G / 255.0;
        double b = color.B / 255.0;

        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        double lightness = (max + min) / 2;

        return (byte)(lightness * 127); // Skaliert auf 0-127
    }

}