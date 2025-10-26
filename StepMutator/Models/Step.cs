using Prism.Mvvm;

namespace StepMutator.Models;

public class Step : BindableBase, IStep
{
    public Step(bool on, bool tie, byte pitch, byte velocity, ushort pitchbend, byte modWheel)
    {
        On = on;
        Tie = tie;
        Pitch = pitch;
        Velocity = velocity;
        Pitchbend = pitchbend;
        ModWheel = modWheel;
    }

    public Step(ulong value)
    {
        Decode(value);
    }

    public ulong Value
    {
        get => Encode();
        set => Decode(value);
    }

    private bool _on;
    public bool On
    {
        get => _on;
        set => SetProperty(ref _on, value);
    }

    private bool _tie;
    public bool Tie
    {
        get => _tie;
        set => SetProperty(ref _tie, value);
    }

    private byte _pitch;

    public byte Pitch
    {
        get => _pitch;
        set => SetProperty(ref _pitch, value);
    }

    private byte _velocity;

    public byte Velocity
    {
        get => _velocity;
        set => SetProperty(ref _velocity, value);
    }

    private ushort _pitchbend;

    public ushort Pitchbend
    {
        get => _pitchbend; 
        set => SetProperty(ref _pitchbend, value);
    }

    private byte _modWheel;

    public byte ModWheel
    {
        get => _modWheel;
        set => SetProperty(ref _modWheel, value);
    }

    private ulong Encode()
    {
        ulong value = 0;
        value |= (On ? 1UL : 0UL) << 0;
        value |= (Tie ? 1UL : 0UL) << 1;
        value |= ((ulong)Pitch & 0x7FUL) << 2;
        value |= ((ulong)Velocity & 0x7FUL) << 9;
        value |= ((ulong)Pitchbend & 0x3FFFUL) << 16;
        value |= ((ulong)ModWheel & 0x7FUL) << 30;
        // Rest bleibt 0
        return value;
    }

    private void Decode(ulong chromosome)
    {
        On = ((chromosome >> 0) & 0x1UL) != 0;
        Tie = ((chromosome >> 1) & 0x1UL) != 0;
        Pitch = (byte)((chromosome >> 2) & 0x7FUL);
        Velocity = (byte)((chromosome >> 9) & 0x7FUL);
        Pitchbend = (ushort)((chromosome >> 16) & 0x3FFFUL);
        ModWheel = (byte)((chromosome >> 30) & 0x7FUL);
    }

}