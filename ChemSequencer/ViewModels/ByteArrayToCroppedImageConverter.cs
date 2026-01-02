using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ChemSequencer.ViewModels;

public sealed class ByteArrayToCroppedImageConverter : IValueConverter
{
    // Padding in Pixeln um das erkannte Objekt herum
    public int Padding { get; set; } = 6;

    // Optional feste Toleranz (0-255). Wenn null, wird automatisch ermittelt.
    public int? FixedTolerance { get; set; }

    // Größe des Sample-Blocks in den Ecken (in Pixeln)
    public int CornerSampleSize { get; set; } = 8;

    // Schwellwert für sehr dunkle Bereiche (0..1)
    public double DarkLuminanceThreshold { get; set; } = 0.18;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not byte[] bytes || bytes.Length == 0)
            return null;

        try
        {
            using var ms = new MemoryStream(bytes);
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.CreateOptions = BitmapCreateOptions.None;
            bmp.StreamSource = ms;
            bmp.EndInit();
            bmp.Freeze();

            // Vereinheitliche Pixelformat für einfaches Scannen
            var fb = new FormatConvertedBitmap(bmp, PixelFormats.Bgra32, null, 0);
            int w = fb.PixelWidth;
            int h = fb.PixelHeight;
            int stride = w * 4;
            var pixels = new byte[h * stride];
            fb.CopyPixels(pixels, stride, 0);

            // Hintergrund aus den Ecken sampeln
            (double r, double g, double b, double a) bgColor = (0, 0, 0, 0);
            var cornerPixels = SampleCorners(pixels, w, h, stride, CornerSampleSize);
            if (cornerPixels.Length == 0)
                return bmp;

            bgColor.r = cornerPixels.Average(p => p.r);
            bgColor.g = cornerPixels.Average(p => p.g);
            bgColor.b = cornerPixels.Average(p => p.b);
            bgColor.a = cornerPixels.Average(p => p.a);

            // automatische Toleranz: max Abstand in Samples + Sicherheitsmargin
            int tolerance = FixedTolerance ?? ComputeAdaptiveTolerance(cornerPixels, bgColor);

            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            for (int y = 0; y < h; y++)
            {
                int row = y * stride;
                for (int x = 0; x < w; x++)
                {
                    int i = row + x * 4;
                    byte pb = pixels[i + 0];
                    byte pg = pixels[i + 1];
                    byte pr = pixels[i + 2];
                    byte pa = pixels[i + 3];

                    bool isForeground;
                    if (pa < 250)
                    {
                        isForeground = true;
                    }
                    else
                    {
                        var dist = ColorDistance(pr, pg, pb, bgColor.r, bgColor.g, bgColor.b);
                        isForeground = dist > tolerance;
                    }

                    if (isForeground)
                    {
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            // Keine Vordergrundpixel gefunden -> Originalbild zurückgeben
            if (minX == int.MaxValue)
                return bmp;

            // Padding anwenden und begrenzen
            int pad = Math.Max(0, Padding);
            int cropX = Math.Max(0, minX - pad);
            int cropY = Math.Max(0, minY - pad);
            int cropW = Math.Min(w - cropX, (maxX - minX) + 1 + pad * 2);
            int cropH = Math.Min(h - cropY, (maxY - minY) + 1 + pad * 2);

            var rect = new Int32Rect(cropX, cropY, cropW, cropH);

            // Kopiere gecroppte Pixel
            int cropStride = cropW * 4;
            var croppedPixels = new byte[cropH * cropStride];
            fb.CopyPixels(rect, croppedPixels, cropStride, 0);

            // Verarbeite Pixel: mache Hintergrund transparent, invertiere sehr dunkle Bereiche (Farbton beibehalten)
            var outPixels = new byte[cropH * cropStride];
            for (int y = 0; y < cropH; y++)
            {
                int row = y * cropStride;
                for (int x = 0; x < cropW; x++)
                {
                    int idx = row + x * 4;
                    byte b = croppedPixels[idx + 0];
                    byte g = croppedPixels[idx + 1];
                    byte r = croppedPixels[idx + 2];
                    byte a = croppedPixels[idx + 3];

                    // Distanz zum Hintergrund
                    double dist = ColorDistance(r, g, b, bgColor.r, bgColor.g, bgColor.b);

                    // Alpha-Fading: weiche Kante zwischen Hintergrund und Vordergrund
                    double alphaFactor;
                    if (a < 250)
                    {
                        alphaFactor = 1.0;
                    }
                    else if (dist <= tolerance * 0.25)
                    {
                        alphaFactor = 0.0;
                    }
                    else
                    {
                        // linear von 0..1 zwischen 0.25*tolerance .. tolerance
                        alphaFactor = (dist - (tolerance * 0.25)) / (Math.Max(1, tolerance * 0.75));
                        alphaFactor = Math.Clamp(alphaFactor, 0.0, 1.0);
                    }

                    if (alphaFactor <= 0.0)
                    {
                        // transparent
                        outPixels[idx + 0] = 0;
                        outPixels[idx + 1] = 0;
                        outPixels[idx + 2] = 0;
                        outPixels[idx + 3] = 0;
                        continue;
                    }

                    // Farbe normalisieren 0..1
                    double rd = r / 255.0;
                    double gd = g / 255.0;
                    double bd = b / 255.0;

                    // Luminanz
                    double lum = (0.30 * rd + 0.59 * gd + 0.11 * bd);

                    // Wenn sehr dunkel -> invertiere Helligkeit, erhalte Farbton (H und S)
                    if (lum < DarkLuminanceThreshold)
                    {
                        var (hh, ss, ll) = RgbToHsl(r, g, b);
                        // invertiere Lightness, aber setze ein Minimum/Maximum für gute Lesbarkeit
                        double newL = Math.Clamp(1.0 - ll, 0.6, 0.98);
                        double newS = Math.Clamp(ss * 1.05, 0.0, 1.0); // leicht saturieren
                        var (rr, gg, bb2) = HslToRgb(hh, newS, newL);
                        r = (byte)Math.Clamp((int)Math.Round(rr * 255.0), 0, 255);
                        g = (byte)Math.Clamp((int)Math.Round(gg * 255.0), 0, 255);
                        b = (byte)Math.Clamp((int)Math.Round(bb2 * 255.0), 0, 255);
                    }

                    // schreibe mit berechneter Alpha
                    byte outA = (byte)Math.Clamp((int)Math.Round(alphaFactor * 255.0), 0, 255);
                    outPixels[idx + 0] = b;
                    outPixels[idx + 1] = g;
                    outPixels[idx + 2] = r;
                    outPixels[idx + 3] = outA;
                }
            }

            // Erzeuge WriteableBitmap und gebe zurück
            var result = new WriteableBitmap(cropW, cropH, fb.DpiX, fb.DpiY, PixelFormats.Bgra32, null);
            result.WritePixels(new Int32Rect(0, 0, cropW, cropH), outPixels, cropStride, 0);
            result.Freeze();
            return result;
        }
        catch
        {
            return null;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static (double r, double g, double b, double a)[] SampleCorners(byte[] pixels, int w, int h, int stride, int sampleSize)
    {
        var list = new System.Collections.Generic.List<(double r, double g, double b, double a)>();
        int s = Math.Max(1, Math.Min(sampleSize, Math.Min(w, h) / 4));

        void SampleBlock(int startX, int startY)
        {
            for (int y = startY; y < Math.Min(h, startY + s); y++)
            {
                int row = y * stride;
                for (int x = startX; x < Math.Min(w, startX + s); x++)
                {
                    int i = row + x * 4;
                    byte bb = pixels[i + 0];
                    byte bg = pixels[i + 1];
                    byte br = pixels[i + 2];
                    byte ba = pixels[i + 3];
                    list.Add((br, bg, bb, ba));
                }
            }
        }

        SampleBlock(0, 0); // oben links
        SampleBlock(w - s, 0); // oben rechts
        SampleBlock(0, h - s); // unten links
        SampleBlock(w - s, h - s); // unten rechts

        return list.ToArray();
    }

    private static int ComputeAdaptiveTolerance((double r, double g, double b, double a)[] samples, (double r, double g, double b, double a) bg)
    {
        // maximale Abweichung eines Samples zum Mittelwert (Euclid, auf 0..255)
        double maxDist = samples.Max(p => ColorDistance(p.r, p.g, p.b, bg.r, bg.g, bg.b));
        // Sicherheitsmargin
        int tol = (int)Math.Ceiling(maxDist + 12); // 12 Pixel-Margin standard
        return Math.Clamp(tol, 6, 120);
    }

    private static double ColorDistance(double r1, double g1, double b1, double r2, double g2, double b2)
    {
        // gewichtete euklidische Distanz (Luminance-gewichtet)
        double dr = r1 - r2;
        double dg = g1 - g2;
        double db = b1 - b2;
        // Gewichte, damit menschliche Wahrnehmung berücksichtigt wird
        return Math.Sqrt(0.30 * dr * dr + 0.59 * dg * dg + 0.11 * db * db);
    }

    // RGB (0..255) -> HSL (h:0..1, s:0..1, l:0..1)
    private static (double h, double s, double l) RgbToHsl(byte r, byte g, byte b)
    {
        double rd = r / 255.0;
        double gd = g / 255.0;
        double bd = b / 255.0;
        double max = Math.Max(rd, Math.Max(gd, bd));
        double min = Math.Min(rd, Math.Min(gd, bd));
        double h = 0.0;
        double s = 0.0;
        double l = (max + min) / 2.0;

        if (Math.Abs(max - min) < 1e-9)
        {
            h = 0.0;
            s = 0.0;
        }
        else
        {
            double d = max - min;
            s = l > 0.5 ? d / (2.0 - max - min) : d / (max + min);
            if (max == rd) h = (gd - bd) / d + (gd < bd ? 6.0 : 0.0);
            else if (max == gd) h = (bd - rd) / d + 2.0;
            else h = (rd - gd) / d + 4.0;
            h /= 6.0;
        }

        return (h, s, l);
    }

    // HSL (h:0..1, s:0..1, l:0..1) -> RGB (0..1)
    private static (double r, double g, double b) HslToRgb(double h, double s, double l)
    {
        if (s <= 0.000001)
        {
            return (l, l, l);
        }
        double q = l < 0.5 ? l * (1.0 + s) : l + s - l * s;
        double p = 2.0 * l - q;
        double r = HueToRgb(p, q, h + 1.0 / 3.0);
        double g = HueToRgb(p, q, h);
        double b = HueToRgb(p, q, h - 1.0 / 3.0);
        return (r, g, b);
    }

    private static double HueToRgb(double p, double q, double t)
    {
        if (t < 0) t += 1.0;
        if (t > 1) t -= 1.0;
        if (t < 1.0 / 6.0) return p + (q - p) * 6.0 * t;
        if (t < 1.0 / 2.0) return q;
        if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6.0;
        return p;
    }
}