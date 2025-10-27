namespace StepMutator.Models;

public class ExtendedNote : INote
{
    public ExtendedNote(bool pause, byte pitch, byte velocity, int length, double cents, byte mod)
    {
        Pause = pause;
        Pitch = pitch;
        Velocity = velocity;
        Length = length;
        Cents = cents;
        Mod = mod;
    }
    public bool Pause { get; }
    public byte Pitch { get; }
    public byte Velocity { get; }
    public int Length { get; }
    public double Cents { get; }
    public byte Mod { get; }
}