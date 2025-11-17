using System;

namespace StepMutator.Models;

public static class PitchbendHelpers
{
    public const int RawCenter = 8192;
    public const int RawMin = 0;
    public const int RawMax = 16383;
    private const double SignedMax = 8192.0; // magnitude used for scaling (-8192..+8191)

    public static int RawToSigned(int raw) => raw - RawCenter; // -8192 .. +8191

    public static int SignedToRaw(int signed) => (int)Math.Clamp(signed + RawCenter, RawMin, RawMax);

    // raw -> semitone offset (z.B. -0.5 .. +0.5 bei rangeSemitones == 0.5)
    public static double RawToSemitoneOffset(int raw, double rangeSemitones)
    {
        return (RawToSigned(raw) / SignedMax) * rangeSemitones;
    }

    // raw -> cents (z.B. -50 .. +50 bei rangeSemitones == 0.5)
    public static double RawToCents(int raw, double rangeSemitones)
    {
        return RawToSemitoneOffset(raw, rangeSemitones) * 100.0;
    }

    // cents -> raw (hilfreich, falls du aus UI Cents zurück in ein PitchBend-Event brauchst)
    public static int CentsToRaw(double cents, double rangeSemitones)
    {
        if (rangeSemitones == 0) return RawCenter;
        double semitones = cents / 100.0;
        double fraction = semitones / rangeSemitones;
        double signed = Math.Round(fraction * SignedMax);
        return SignedToRaw((int)signed);
    }
}