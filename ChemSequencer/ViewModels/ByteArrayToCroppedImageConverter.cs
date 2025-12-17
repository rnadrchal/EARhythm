using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ChemSequencer.ViewModels
{
    public sealed class ByteArrayToCroppedImageConverter : IValueConverter
    {
        // Padding in Pixeln um das erkannte Objekt herum
        public int Padding { get; set; } = 6;

        // Optional feste Toleranz (0-255). Wenn null, wird automatisch ermittelt.
        public int? FixedTolerance { get; set; }

        // Größe des Sample-Blocks in den Ecken (in Pixeln)
        public int CornerSampleSize { get; set; } = 8;

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
                (double r, double g, double b, double a) bg = (0, 0, 0, 0);
                var cornerPixels = SampleCorners(pixels, w, h, stride, CornerSampleSize);
                if (cornerPixels.Length == 0)
                    return bmp;

                bg.r = cornerPixels.Average(p => p.r);
                bg.g = cornerPixels.Average(p => p.g);
                bg.b = cornerPixels.Average(p => p.b);
                bg.a = cornerPixels.Average(p => p.a);

                // automatische Toleranz: max Abstand in Samples + Sicherheitsmargin
                int tolerance = FixedTolerance ?? ComputeAdaptiveTolerance(cornerPixels, bg);

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
                            var dist = ColorDistance(pr, pg, pb, bg.r, bg.g, bg.b);
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
                var cropped = new CroppedBitmap(fb, rect);
                cropped.Freeze();
                return cropped;
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
    }
}