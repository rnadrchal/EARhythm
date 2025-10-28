using System.Windows.Media;
using Egami.Imaging.Extensions;

namespace Egami.Imaging.Midi;

public class HueToCv : IColorToCv
{
    public byte Convert(Color color)
    {
        return color.HueSevenBit();
    }
}