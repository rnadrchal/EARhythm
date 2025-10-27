
using System.Windows.Media;

namespace ImageSequencer.Services;

public interface IBitmapWalker
{
    Color Next();
}