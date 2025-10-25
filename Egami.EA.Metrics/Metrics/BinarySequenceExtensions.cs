using MathNet.Numerics.IntegralTransforms;

namespace Egami.EA.Metrics.Metrics;

public static class BinarySequenceExtensions
{
    private static double[] PadToLength(double[] input, int minLength)
    {
        if (input.Length >= minLength)
            return input;
        var padded = new double[minLength];
        Array.Copy(input, padded, input.Length);
        // Die restlichen Werte bleiben 0
        return padded;
    }

    public static double CombinedRhythmSimilarity(this IEnumerable<bool> a, IEnumerable<bool> b)
    {
        var arrA = a.ToArray();
        var arrB = b.ToArray();
        int maxLength = Math.Max(arrA.Length, arrB.Length);

        // Beide Sequenzen auf gleiche Länge bringen
        bool[] arrAPadded = new bool[maxLength];
        bool[] arrBPadded = new bool[maxLength];
        Array.Copy(arrA, arrAPadded, arrA.Length);
        Array.Copy(arrB, arrBPadded, arrB.Length);

        // Hamming-Distanz
        int differences = 0;
        for (int i = 0; i < maxLength; i++)
            if (arrAPadded[i] != arrBPadded[i]) differences++;
        double hammingSimilarity = 1.0 - (double)differences / maxLength;

        // FFT (Spektrum) mit Padding
        const int fftMinLength = 8;
        int fftLength = Math.Max(maxLength, fftMinLength);
        double[] aDouble = PadToLength(arrAPadded.Select(x => x ? 1.0 : 0.0).ToArray(), fftLength + 2);
        double[] bDouble = PadToLength(arrBPadded.Select(x => x ? 1.0 : 0.0).ToArray(), fftLength + 2);
        var aFft = (double[])aDouble.Clone();
        var bFft = (double[])bDouble.Clone();
        Fourier.ForwardReal(aFft, fftLength);
        Fourier.ForwardReal(bFft, fftLength);

        // Amplituden vergleichen
        double[] aAmp = aFft.Select(Math.Abs).ToArray();
        double[] bAmp = bFft.Select(Math.Abs).ToArray();

        // Cosinus-Ähnlichkeit des Spektrums
        double dot = 0, normA = 0, normB = 0;
        for (int i = 0; i < aAmp.Length; i++)
        {
            dot += aAmp[i] * bAmp[i];
            normA += aAmp[i] * aAmp[i];
            normB += bAmp[i] * bAmp[i];
        }
        double spectralSimilarity = (normA == 0 || normB == 0) ? 0 : dot / (Math.Sqrt(normA) * Math.Sqrt(normB));

        // Kombinieren (z.B. gleich gewichtet)
        return 0.5 * hammingSimilarity + 0.5 * spectralSimilarity;
    }
}