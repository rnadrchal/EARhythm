using System.Windows.Media;
using Egami.Imaging.Extensions;

namespace Egami.Imaging.Midi;

public sealed class SaturationToCv : IColorToCv
{
    public byte Convert(Color color)
    {
        return color.SaturationSevenBit();
    }
}