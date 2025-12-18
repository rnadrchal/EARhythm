namespace Egami.Chemistry.Spectrum;

public readonly record struct MidiPitchSigned(
    int Note,               // 0..127
    int PitchBendSigned,    // -8192..+8192 (0 = neutral)
    double AudioFrequencyHz,
    double MidiFloat,
    double CentsOff
);

public sealed class WavelengthToMidiPitchMapper
{
    private const double C = 299_792_458.0; // m/s

    private readonly double _a4Hz;
    private readonly int _a4Midi;
    private readonly double _pitchBendRangeSemitones;

    public WavelengthToMidiPitchMapper(
        double pitchBendRangeSemitones = 2.0,
        double a4Hz = 440.0,
        int a4Midi = 69)
    {
        if (pitchBendRangeSemitones <= 0) throw new ArgumentOutOfRangeException(nameof(pitchBendRangeSemitones));
        if (a4Hz <= 0) throw new ArgumentOutOfRangeException(nameof(a4Hz));
        if (a4Midi is < 0 or > 127) throw new ArgumentOutOfRangeException(nameof(a4Midi));

        _pitchBendRangeSemitones = pitchBendRangeSemitones;
        _a4Hz = a4Hz;
        _a4Midi = a4Midi;
    }

    /// <summary>
    /// Mappt eine Wellenlänge im sichtbaren Bereich auf einen akustischen Bereich,
    /// der über MIDI-Noten (min/max) definiert ist.
    ///
    /// Default visible range: 380..780 nm.
    /// Pitchbend output: -8192..+8192 (0 = neutral).
    /// </summary>
    public MidiPitchSigned MapWavelengthNmToMidi(
        double wavelengthNm,
        int midiMinNote,
        int midiMaxNote,
        double visibleMinNm = 380.0,
        double visibleMaxNm = 780.0)
    {
        if (wavelengthNm <= 0) throw new ArgumentOutOfRangeException(nameof(wavelengthNm));
        if (midiMinNote is < 0 or > 127) throw new ArgumentOutOfRangeException(nameof(midiMinNote));
        if (midiMaxNote is < 0 or > 127) throw new ArgumentOutOfRangeException(nameof(midiMaxNote));
        if (midiMinNote > midiMaxNote) throw new ArgumentException("midiMinNote must be <= midiMaxNote.");
        if (visibleMinNm <= 0 || visibleMaxNm <= 0) throw new ArgumentOutOfRangeException("visible range must be > 0.");
        if (visibleMinNm >= visibleMaxNm) throw new ArgumentException("visibleMinNm must be < visibleMaxNm.");

        // 1) λ -> optische Frequenz
        var nu = OpticalFrequencyHzFromNm(wavelengthNm);

        // 2) sichtbarer Bereich als optische Frequenzen (nu_min..nu_max)
        // Achtung: nu = c/λ => kleinere λ => größere nu
        var nuMin = OpticalFrequencyHzFromNm(visibleMaxNm); // bei max λ ist nu minimal
        var nuMax = OpticalFrequencyHzFromNm(visibleMinNm); // bei min λ ist nu maximal

        // 3) Audio-Fenster über MIDI-Notes definieren
        var fMin = MidiNoteToFrequencyHz(midiMinNote);
        var fMax = MidiNoteToFrequencyHz(midiMaxNote);

        // 4) log-log Mapping: nu (optisch) -> f (audio)
        var t = NormalizeLog(nu, nuMin, nuMax); // 0..1
        var audioHz = InterpLog(fMin, fMax, t);

        // 5) AudioHz -> MIDI note + signed pitchbend
        return MapAudioFrequencyHzToMidi(audioHz, midiMinNote, midiMaxNote);
    }

    public MidiPitchSigned MapAudioFrequencyHzToMidi(double audioHz, int minNote = 0, int maxNote = 127)
    {
        if (audioHz <= 0) throw new ArgumentOutOfRangeException(nameof(audioHz));
        if (minNote is < 0 or > 127) throw new ArgumentOutOfRangeException(nameof(minNote));
        if (maxNote is < 0 or > 127) throw new ArgumentOutOfRangeException(nameof(maxNote));
        if (minNote > maxNote) throw new ArgumentException("minNote must be <= maxNote.");

        var midiFloat = _a4Midi + 12.0 * Log2(audioHz / _a4Hz);

        var note = (int)Math.Round(midiFloat, MidpointRounding.AwayFromZero);
        note = Clamp(note, minNote, maxNote);

        var semitoneOffset = midiFloat - note;      // typ. -0.5..+0.5
        var centsOff = semitoneOffset * 100.0;

        // signed PB: -8192..+8192, 0 = center
        var fraction = semitoneOffset / _pitchBendRangeSemitones;
        var pbSigned = (int)Math.Round(fraction * 8192.0, MidpointRounding.AwayFromZero);
        pbSigned = Clamp(pbSigned, -8192, 8192);

        return new MidiPitchSigned(note, pbSigned, audioHz, midiFloat, centsOff);
    }

    public double MidiNoteToFrequencyHz(int note)
    {
        if (note is < 0 or > 127) throw new ArgumentOutOfRangeException(nameof(note));
        return _a4Hz * Math.Pow(2.0, (note - _a4Midi) / 12.0);
    }

    private static double OpticalFrequencyHzFromNm(double nm)
    {
        var m = nm * 1e-9;
        return C / m;
    }

    private static double NormalizeLog(double x, double min, double max)
    {
        // clamp + log mapping
        var xc = Math.Clamp(x, min, max);
        var lx = Math.Log(xc);
        var lmin = Math.Log(min);
        var lmax = Math.Log(max);
        return (lx - lmin) / (lmax - lmin);
    }

    private static double InterpLog(double a, double b, double t)
    {
        // exp( log(a) + t*(log(b)-log(a)) )
        var la = Math.Log(a);
        var lb = Math.Log(b);
        return Math.Exp(la + t * (lb - la));
    }

    private static double Log2(double x) => Math.Log(x) / Math.Log(2.0);

    private static int Clamp(int v, int min, int max)
        => v < min ? min : (v > max ? max : v);
}
