using Egami.Rhythm.Core;

namespace Egami.Rhythm;

public sealed class RhythmContext
{
    public required int StepsTotal { get; init; }           // Gesamtlänge im Raster (z.B. 16)
    public required byte DefaultVelocity { get; set; }      // z.B. 100 (0 - 127)
    public required Meter Meter { get; init; }              // z.B. 4/4
    public required Timebase Timebase { get; init; }        // z.B. SPQ=4
    public double? TargetDensity01 { get; init; }           // optional: 0..1 durchschnittliche Trefferquote
    public double TempoBpm { get; init; } = 120.0;          // hilfreich für MIDI-Render
    public int? Seed { get; init; } = null;                 // optionaler Seed für RNG
}