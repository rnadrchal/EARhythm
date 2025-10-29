using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace Egami.Imaging.Extensions;

public static class BitmapTransform
{
    public static WriteableBitmap ScaleBitmap(
        this WriteableBitmap source,
        double scale)
    {
        int scaledWidth = (int)Math.Max(1, source.PixelWidth * scale);
        int scaledHeight = (int)Math.Max(1, source.PixelHeight * scale);

        // Verwende das ursprüngliche PixelFormat des Quellbildes
        PixelFormat targetFormat = source.Format;

        // RenderTargetBitmap unterstützt nicht alle Formate, daher immer Pbgra32 verwenden
        var rtb = new RenderTargetBitmap(scaledWidth, scaledHeight, source.DpiX, source.DpiY, PixelFormats.Pbgra32);
        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            dc.DrawImage(source, new System.Windows.Rect(0, 0, scaledWidth, scaledHeight));
        }
        rtb.Render(dv);

        // Schreibe die Daten in ein neues WriteableBitmap mit dem ursprünglichen Format
        var result = new WriteableBitmap(scaledWidth, scaledHeight, source.DpiX, source.DpiY, targetFormat, source.Palette);

        // Pixeldaten kopieren
        int stride = (scaledWidth * targetFormat.BitsPerPixel + 7) / 8;
        byte[] buffer = new byte[stride * scaledHeight];
        rtb.CopyPixels(buffer, stride, 0);
        result.WritePixels(new System.Windows.Int32Rect(0, 0, scaledWidth, scaledHeight), buffer, stride, 0);

        return result;
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
        int width = source.PixelWidth;
        int height = source.PixelHeight;

        // Ziel-WriteableBitmap mit Gray8-Format
        var grayBitmap = new WriteableBitmap(
            source.PixelWidth, source.PixelHeight, source.DpiX, source.DpiY, PixelFormats.Gray8, null);

        // Quellbild in Pbgra32 rendern (falls nötig)
        var rtb = new RenderTargetBitmap(width, height, source.DpiX, source.DpiY, PixelFormats.Pbgra32);
        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            dc.DrawImage(source, new System.Windows.Rect(0, 0, width, height));
        }
        rtb.Render(dv);

        // Pixeldaten holen
        byte[] srcBuffer = new byte[width * height * 4];
        rtb.CopyPixels(srcBuffer, width * 4, 0);

        // Zielpuffer für Gray8
        byte[] grayBuffer = new byte[width * height];

        for (int i = 0; i < width * height; i++)
        {
            int idx = i * 4;
            byte r = srcBuffer[idx + 2];
            byte g = srcBuffer[idx + 1];
            byte b = srcBuffer[idx + 0];
            // Luminanz-Berechnung
            byte luminance = (byte)((r * 299 + g * 587 + b * 114) / 1000);
            grayBuffer[i] = luminance;
        }

        // In das Zielbild schreiben
        int stride = width;
        grayBitmap.WritePixels(new System.Windows.Int32Rect(0, 0, width, height), grayBuffer, stride, 0);

        return grayBitmap;
    }

}
