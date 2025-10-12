namespace Egami.Rhythm.Pattern;

/// <summary>
/// Ein einzelnes Ereignis im Raster (Grid-orientiert, optional Länge/Velocity)
/// </summary>
/// <param name="Step"></param>
/// <param name="Velocity"></param>
/// <param name="Length"></param>
public readonly record struct RhythmEvent(int Step, byte Velocity = 100, int Length = 1, int? Pitch = null);