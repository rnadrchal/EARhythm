namespace StepMutator.Models;

public interface IStep
{
    ulong Value { get; set; }
    bool On { get; }
    bool Tie { get; }
    byte Pitch { get; }
    byte Velocity { get; }
    ushort Pitchbend { get; }
    byte ModWheel { get; }

}