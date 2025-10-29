using System.Windows.Media;
using Egami.Imaging.Extensions;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;

namespace Egami.Imaging.Midi;

public sealed class BaseColorToCv : IColorToCv
{
    private readonly BaseColor _baseColor;

    public BaseColorToCv(BaseColor baseColor)
    {
        _baseColor = baseColor;
    }

    public byte Convert(Color color)
    {
        var pitch = color.BaseColorSevenBit(_baseColor);
        return pitch;
    }

}