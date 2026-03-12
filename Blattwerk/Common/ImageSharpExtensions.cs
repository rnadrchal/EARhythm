using System;
using System.Collections.Generic;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Blattwerk.Common;

/// <summary>
/// Statistische, normalisierte Werte eines Segments.
/// Alle Werte liegen im Bereich [0, 1].
/// </summary>
public sealed class SegmentColorStats
{
    public int Index { get; init; }
    public int PixelCount { get; init; }

    public float A { get; init; }
    public float R { get; init; }
    public float G { get; init; }
    public float B { get; init; }

    public float Saturation { get; init; } // HSL Saturation
    public float Luminance { get; init; }  // HSL L
    public float RecipWeightedBrightness { get; init; } // 1 - (0.2126*R + 0.7152*G + 0.0722*B)

    public float Y => 1f - B; // Yellow = 1 - Blue
    public float M => 1f - G; // Magenta = 1 - Green
    public float C => 1f - R; // Cyan = 1 - Red
}

public static class ImageSharpExtensions
{
    /// <summary>
    /// Liest die Spalte mit Index <paramref name="x"/> als Array von <see cref="Argb32"/>.
    /// </summary>
    public static Argb32[] GetColumn(this Image<Argb32> image, int x)
    {
        if (image is null) throw new ArgumentNullException(nameof(image));
        if (x < 0 || x >= image.Width) throw new ArgumentOutOfRangeException(nameof(x));
        var result = new Argb32[image.Height];
        for (int y = 0; y < image.Height; y++)
        {
            result[y] = image[x, y];
        }
        return result;
    }

    /// <summary>
    /// Liest die Zeile mit Index <paramref name="y"/> als Array von <see cref="Rgba32"/>.
    /// </summary>
    public static Rgba32[] GetRow(this Image<Rgba32> image, int y)
    {
        if (image is null) throw new ArgumentNullException(nameof(image));
        if (y < 0 || y >= image.Height) throw new ArgumentOutOfRangeException(nameof(y));
        var result = new Rgba32[image.Width];
        image.ProcessPixelRows(accessor =>
        {
            var span = accessor.GetRowSpan(y);
            span.CopyTo(result);
        });
        return result;
    }

    /// <summary>
    /// Normalisierte Komponenten aus einem einzelnen Pixel (Rgba32).
    /// Rückgabe: (a, r, g, b) jeweils in [0,1].
    /// </summary>
    public static (float a, float r, float g, float b) ToNormalized(this Rgba32 px)
    {
        const float inv = 1f / 255f;
        return (px.A * inv, px.R * inv, px.G * inv, px.B * inv);
    }

    /// <summary>
    /// Teilt die Pixel-Sequenz in segmentCount gleichmäßig verteilte, zusammenhängende Segmente
    /// und berechnet pro Segment normalisierte Durchschnittswerte.
    /// Erwartet nun IEnumerable&lt;Rgba32&gt; (Byte-Reihenfolge R,G,B,A).
    /// </summary>
    public static SegmentColorStats[] ComputeSegmentAverages(this IEnumerable<Rgba32> pixels, int segmentCount)
    {
        if (pixels is null) throw new ArgumentNullException(nameof(pixels));
        if (segmentCount <= 0) throw new ArgumentOutOfRangeException(nameof(segmentCount));

        var arr = pixels as Rgba32[] ?? pixels.ToArray();
        int total = arr.Length;
        var result = new SegmentColorStats[segmentCount];

        // gleichmäßige Aufteilung: start = i*total/segmentCount .. end = (i+1)*total/segmentCount - 1
        for (int i = 0; i < segmentCount; i++)
        {
            int start = (int)((long)i * total / segmentCount);
            int end = (int)((long)(i + 1) * total / segmentCount); // exclusive
            int count = Math.Max(0, end - start);

            double sumA = 0, sumR = 0, sumG = 0, sumB = 0;

            for (int j = start; j < end; j++)
            {
                var px = arr[j];
                sumA += px.A;
                sumR += px.R;
                sumG += px.G;
                sumB += px.B;
            }

            float aAvg = 0, rAvg = 0, gAvg = 0, bAvg = 0;
            if (count > 0)
            {
                const float inv255 = 1f / 255f;
                aAvg = (float)(sumA / count) * inv255;
                rAvg = (float)(sumR / count) * inv255;
                gAvg = (float)(sumG / count) * inv255;
                bAvg = (float)(sumB / count) * inv255;
            }

            // HSL L und Saturation basierend auf durchschnittlichem RGB
            float max = MathF.Max(rAvg, MathF.Max(gAvg, bAvg));
            float min = MathF.Min(rAvg, MathF.Min(gAvg, bAvg));
            float delta = max - min;

            float l = (max + min) / 2f;
            float s;
            if (delta == 0f)
            {
                s = 0f;
            }
            else
            {
                // HSL saturation
                s = delta / (1f - MathF.Abs(2f * l - 1f));
            }

            // gewichtete lineare Helligkeit (Luma), Standardkoeffizienten für sRGB Luminance
            float weightedBrightness = 0.2126f * rAvg + 0.7152f * gAvg + 0.0722f * bAvg;
            float recipBrightness = 1f - weightedBrightness;

            // Clamp in [0,1]
            s = Clamp(s, 0f, 1f);
            l = Clamp(l, 0f, 1f);
            recipBrightness = Clamp(recipBrightness, 0f, 1f);

            result[i] = new SegmentColorStats
            {
                Index = i,
                PixelCount = count,
                A = Clamp(aAvg, 0f, 1f),
                R = Clamp(rAvg, 0f, 1f),
                G = Clamp(gAvg, 0f, 1f),
                B = Clamp(bAvg, 0f, 1f),
                Saturation = s,
                Luminance = l,
                RecipWeightedBrightness = recipBrightness
            };
        }

        return result;
    }

    static float Clamp(float value, float min, float max) => value < min ? min : (value > max ? max : value);
}