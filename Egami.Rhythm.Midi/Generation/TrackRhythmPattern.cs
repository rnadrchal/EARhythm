using Egami.Rhythm.Pattern;
using Melanchall.DryWetMidi.Standards;

namespace Egami.Rhythm.Midi.Generation;

public class TrackRhythmPattern
{
    public TrackRhythmPattern(int number, Sequence pattern, double drumLikeliyhood, int channel, byte[] programs)
    {
        RhythmPattern = pattern;
        DrumLikelihood = drumLikeliyhood;
        Channel = channel;
        Programs = programs;
    }
    public int TrackNumber { get; private set; }
    public int Channel { get; private set; }
    public double DrumLikelihood { get; private set; }
    public Sequence RhythmPattern { get; private set; }
    public byte[] Programs { get; private set; }

    public override string ToString()
    {
        if (DrumLikelihood > 0.9)
        {
            return $"Percussion ({(DrumLikelihood * 100):N0}%)";
        }
        return string.Join(", ", Programs.Select(p => ((GeneralMidiProgram)p).ToString()));
    }
}