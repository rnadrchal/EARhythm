using System;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace Egami.Imaging.Extensions;

public static class BitmapTransform
{
    public static WriteableBitmap ScaleBitmap(
        this WriteableBitmap source,
        double scale)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (scale <= 0) throw new ArgumentOutOfRangeException(nameof(scale), "Scale must be > 0");

        int scaledWidth = (int)Math.Max(1, Math.Round(source.PixelWidth * scale));
        int scaledHeight = (int)Math.Max(1, Math.Round(source.PixelHeight * scale));

        // Wichtig: RenderTargetBitmap in 96 DPI erzeugen, damit DIPs == Pixel und
        // das DrawImage-Rect (in Pixelwerten) korrekt das gesamte Ziel füllt.
        var rtb = new RenderTargetBitmap(scaledWidth, scaledHeight, 96, 96, PixelFormats.Pbgra32);

        var dv = new DrawingVisual();
        // hochwertige Skalierung verwenden
        RenderOptions.SetBitmapScalingMode(dv, BitmapScalingMode.HighQuality);

        using (var dc = dv.RenderOpen())
        {
            // Rechteck in DIPs; bei 96 DPI entspricht ein DIP genau einem Pixel.
            dc.DrawImage(source, new System.Windows.Rect(0, 0, scaledWidth, scaledHeight));
        }

        rtb.Render(dv);

        // Direkt aus dem gerenderten Bitmap ein WriteableBitmap erzeugen.
        return new WriteableBitmap(rtb);
    }

    /// <summary>
    /// Cropt ein Bitmap nach den angegebenen Parametern.
    /// </summary>
    public static WriteableBitmap CropBitmap(
        this WriteableBitmap source,
        int centerX,
        int centerY,
        int scaleX,
        int scaleY,
        double aspectRatio)
    {
        double cropW = scaleX;
        double cropH = scaleY;

        // Seitenverhältnis anpassen
        if (aspectRatio > 0)
        {
            cropH = cropW / aspectRatio;
            if (cropH > scaleY)
            {
                cropH = scaleY;
                cropW = cropH * aspectRatio;
            }
        }

        int x0 = centerX - (int)(cropW / 2);
        int y0 = centerY - (int)(cropH / 2);

        // Begrenzung auf gültige Werte
        x0 = Math.Max(0, Math.Min(x0, source.PixelWidth - (int)cropW));
        y0 = Math.Max(0, Math.Min(y0, source.PixelHeight - (int)cropH));

        var cropped = new CroppedBitmap(source, new System.Windows.Int32Rect(x0, y0, (int)cropW, (int)cropH));
        return new WriteableBitmap(cropped);
    }

    /// <summary>
    /// Berechnet gültige Crop-Koordinaten für ein Bitmap.
    /// </summary>
    public static (int validX, int validY) GetValidCropCoordinates(
        this WriteableBitmap source,
        int centerX,
        int centerY,
        int cropWidth,
        int cropHeight)
    {
        int x0 = centerX - cropWidth / 2;
        int y0 = centerY - cropHeight / 2;

        x0 = Math.Max(0, Math.Min(x0, source.PixelWidth - cropWidth));
        y0 = Math.Max(0, Math.Min(y0, source.PixelHeight - cropHeight));

        return (x0, y0);
    }

    public static WriteableBitmap ToMonochrome(this WriteableBitmap source)
    {
        int width = source.PixelWidth;
        int height = source.PixelHeight;

        // Schritt 1: Quellbild in Pbgra32 rendern
        var rtb = new RenderTargetBitmap(width, height, source.DpiX, source.DpiY, PixelFormats.Pbgra32);
        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            dc.DrawImage(source, new System.Windows.Rect(0, 0, width, height));
        }
        rtb.Render(dv);

        // Schritt 2: Monochrom-WriteableBitmap erzeugen
        var monoBitmap = new WriteableBitmap(source.PixelWidth, source.PixelHeight, source.DpiX, source.DpiY, PixelFormats.BlackWhite, null);

        // Schritt 3: Pixel-Daten holen und Schwellenwert anwenden
        var buffer = new byte[width * height * 4];
        rtb.CopyPixels(buffer, width * 4, 0);

        int bitsPerPixel = monoBitmap.Format.BitsPerPixel; // Für BlackWhite: 1
        int stride = ((width * bitsPerPixel + 31) / 32) * 4; // 32-Bit Alignment
        byte[] monoBuffer = new byte[stride * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int idx = (y * width + x) * 4;
                // Luminanz berechnen (einfache Formel)
                byte r = buffer[idx + 2];
                byte g = buffer[idx + 1];
                byte b = buffer[idx + 0];
                int luminance = (r * 299 + g * 587 + b * 114) / 1000;
                bool isWhite = luminance > 128;

                int bitIndex = y * width + x;
                if (isWhite)
                    monoBuffer[bitIndex / 8] |= (byte)(1 << (7 - (bitIndex % 8)));
            }
        }

        // Schritt 4: Pixel in das Monochrom-Bitmap schreiben
        monoBitmap.WritePixels(
            new System.Windows.Int32Rect(0, 0, width, height),
            monoBuffer, stride, 0);
        return monoBitmap;
    }

    public static WriteableBitmap ToGrayscale(this WriteableBitmap source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        // Wenn bereits Gray8, einfach zurückgeben (neues WriteableBitmap-Objekt)
        if (source.Format == PixelFormats.Gray8)
            return new WriteableBitmap(source);

        try
        {
            // Direkte, explizite Formatkonvertierung — DPI und Größe werden beibehalten
            var converted = new FormatConvertedBitmap(source, PixelFormats.Gray8, null, 0);
            return new WriteableBitmap(converted);
        }
        catch
        {
            // Fallback: Rendern nach Pbgra32 und dann konvertieren
            int width = source.PixelWidth;
            int height = source.PixelHeight;
            var rtb = new RenderTargetBitmap(width, height, source.DpiX, source.DpiY, PixelFormats.Pbgra32);
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                dc.DrawImage(source, new System.Windows.Rect(0, 0, width, height));
            }
            rtb.Render(dv);

            var converted = new FormatConvertedBitmap(rtb, PixelFormats.Gray8, null, 0);
            return new WriteableBitmap(converted);
        }
    }

}
