namespace Egami.Rhythm.EA.Representations;

public class StepChromosome
{
    // Bit-Layout:
    // 0: On/Off (1 Bit)
    // 1: Tie (1 Bit)
    // 2-8: Pitch (7 Bit)
    // 9-15: Velocity (7 Bit)
    // 16-29: Pitchbend (14 Bit)
    // 30-36: ModWheel (7 Bit)
    // 37-63: (frei/reserviert)

    public bool On { get; set; }
    public bool Tie { get; set; }
    public byte Pitch { get; set; }        // 0..127
    public byte Velocity { get; set; }     // 0..127
    public ushort Pitchbend { get; set; }  // 0..16383
    public byte ModWheel { get; set; }     // 0..127

    public StepChromosome() { }

    public StepChromosome(ulong chromosome)
    {
        Decode(chromosome);
    }

    public ulong Encode()
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

    public void Decode(ulong chromosome)
    {
        On = ((chromosome >> 0) & 0x1UL) != 0;
        Tie = ((chromosome >> 1) & 0x1UL) != 0;
        Pitch = (byte)((chromosome >> 2) & 0x7FUL);
        Velocity = (byte)((chromosome >> 9) & 0x7FUL);
        Pitchbend = (ushort)((chromosome >> 16) & 0x3FFFUL);
        ModWheel = (byte)((chromosome >> 30) & 0x7FUL);
    }
}