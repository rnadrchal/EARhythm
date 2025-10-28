using System.Windows.Media;
using Melanchall.DryWetMidi.Core;

namespace Egami.Imaging.Midi;

public interface IColorToCv
{
    byte Convert(Color color);
}