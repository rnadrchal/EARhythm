namespace StepMutator.Models;

public interface INote
{
    bool Pause { get; }
    byte Pitch { get; }
    byte Velocity { get; }
    int Length { get; }
    double Cents { get; }
    byte Mod { get; }
}