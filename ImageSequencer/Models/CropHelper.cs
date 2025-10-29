using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace ImageSequencer.Models;

public static class CropHelper
{
    public static string[] Formats = new[]
    {
        "Original",
        "5:2",
        "2:1",
        "3:2",
        "4:3",
        "5:4",
        "6:5",
        "1:1",
        "5:6",
        "4:5",
        "3:4",
        "2:1",
        "2:5"
    };

    public static (int num, int denom)? Parse(string format)
    {
        var parts = format.Split(':');
        if (parts.Length != 2) return null;
        if (double.TryParse(parts[0], out var num) && double.TryParse(parts[1], out var denom))
        {
            return new ValueTuple<int, int>((int)num, (int)denom);
        }

        return null;
    }

    public static (int w, int h) GetAspectRatio(int width, int height)
    {
        int gcd = GCD(width, height);
        return (width / gcd, height / gcd);
    }

    public static string GetAspectRatioString(int width, int height)
    {
        var (w, h) = GetAspectRatio(width, height);
        return $"{w}:{h}";
    }

    private static int GCD(int a, int b)
    {
        while (b != 0)
        {
            int t = b;
            b = a % b;
            a = t;
        }
        return a;
    }
}