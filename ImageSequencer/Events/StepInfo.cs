using ImageSequencer.Extensions;
using Prism.Events;

namespace ImageSequencer.Events;

public class StepInfo
{
    private static readonly string[] NoteNames = new string[]
    {
        "C ", "C+", "D ", "D+", "E ", "F ", "F+", "G ", "G+", "A ", "A+", "B "
    };

    public int NoteNumber { get; set; }
    public int Velocity { get; set; }
    public int Pitchbend { get; set; } = 2048;
    public int ControlChangeNumber { get; set; }
    public int ControlChangeValue { get; set; }

    public override string ToString()
    {
        var result = $"NN:{NoteNumber.ToNoteNumberString(),-5} VEL:{Velocity:000} PB:{Pitchbend - 2048: 0000;-0000; 0000} CC:{ControlChangeNumber:X2}-{ControlChangeValue:000}";
        return result;
    }
}

public class StepEvent : PubSubEvent<StepInfo>
{
}