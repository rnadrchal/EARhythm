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
}