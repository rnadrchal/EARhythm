using System.Collections.Generic;
using System.Windows.Media;
using System;

namespace ImageSequencer.Extensions;

public static class ColorExtensions
{
    public static Color Average(this IEnumerable<Color> colors)
    {
        if (colors == null) throw new ArgumentNullException(nameof(colors));

        int count = 0;
        long r = 0, g = 0, b = 0, a = 0;

        foreach (var color in colors)
        {
            r += color.R;
            g += color.G;
            b += color.B;
            a += color.A;
            count++;
        }

        if (count == 0)
            return Colors.Transparent;

        return Color.FromArgb(
            (byte)(a / count),
            (byte)(r / count),
            (byte)(g / count),
            (byte)(b / count)
        );
    }
}