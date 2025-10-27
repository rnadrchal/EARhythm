using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ImageSequencer.Services;

public abstract class BitmapWalkerBase : IBitmapWalker
{
    protected readonly WriteableBitmap Bitmap;
    protected readonly int Stride;
    protected int Position;

    protected BitmapWalkerBase(WriteableBitmap bitmap, int stride = 1)
    {
        Bitmap = bitmap ?? throw new ArgumentNullException(nameof(bitmap));
        Stride = stride > 0 ? stride : throw new ArgumentOutOfRangeException(nameof(stride));
        Position = 0;
    }

    public abstract Color Next();
}