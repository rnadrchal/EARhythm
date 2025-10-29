using Egami.Imaging.Extensions;

namespace Egami.Imaging.Midi;

public static class ColorToCvFactory
{
    public static IColorToCv Create(ColorToCvType type, BaseColor baseColor = BaseColor.Red)
    {
        return type switch
        {
            ColorToCvType.Color => new BaseColorToCv(baseColor),
            ColorToCvType.Luminance => new LuminanceToCv(),
            ColorToCvType.Hue => new HueToCv(),
            ColorToCvType.Saturation => new SaturationToCv(),
            ColorToCvType.Brightness => new BrightnessToCv(),
            _ => throw new ArgumentException("Unbekannter ColorToCv-Typ", nameof(type))
        };
    }
}