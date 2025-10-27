using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ImageSequencer.Extensions;

namespace ImageSequencer.Services;

public class RowBitmapWalker : BitmapWalkerBase
{
    private readonly int _width;
    private readonly int _height;

    public RowBitmapWalker(WriteableBitmap bitmap, int stride = 1)
        : base(bitmap, stride)
    {
        _width = bitmap.PixelWidth;
        _height = bitmap.PixelHeight;
    }

    public override Color Next()
    {
        if (Position >= _width * _height)
            throw new InvalidOperationException("No more pixels.");

        var pixels = new List<Color>(Stride);
        for (int i = 0; i < Stride && Position < _width * _height; i++, Position++)
        {
            int x = Position % _width;
            int y = Position / _width;
            pixels.Add(GetPixel(x, y));
        }

        return pixels.Average();
    }

    private Color GetPixel(int x, int y)
    {
        // Assumes PixelFormat = Bgra32
        int stride = Bitmap.BackBufferStride;
        unsafe
        {
            byte* pBackBuffer = (byte*)Bitmap.BackBuffer.ToPointer();
            int index = y * stride + x * 4;
            byte b = pBackBuffer[index + 0];
            byte g = pBackBuffer[index + 1];
            byte r = pBackBuffer[index + 2];
            byte a = pBackBuffer[index + 3];
            return Color.FromArgb(a, r, g, b);
        }
    }
}