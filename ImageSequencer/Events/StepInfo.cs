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
    public int Pitchbend { get; set; } = 8192;
    public int ControlChangeNumber { get; set; }
    public int ControlChangeValue { get; set; }

    public override string ToString()
    {
        var result = $"NN:{GetNoteNumber(NoteNumber),-5} VEL:{Velocity:000} PB:{Pitchbend - 8192: 0000;-0000; 0000} CC:{ControlChangeNumber:X2}-{ControlChangeValue:000}";
        return result;
    }

    private static string GetNoteNumber(int noteNumber)
    {
        if (noteNumber > 0)
        {
            int noteIndex = noteNumber % 12;
            return $"{NoteNames[noteIndex]}{noteNumber / 12 - 1: 00;-00; 00}";
        }
        return string.Empty;
    }
}

public class StepEvent : PubSubEvent<StepInfo>
{
}