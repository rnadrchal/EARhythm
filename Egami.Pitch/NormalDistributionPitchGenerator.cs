using System;
using Egami.Rhythm.Common;

namespace Egami.Pitch;

public class NormalDistributionPitchGenerator : IPitchGenerator
{
    public int? Seed { get; set; } = null;
    public int StandardDeviation { get; set; } = 7; // Quinte
    public double Skewness { get; set; } = 0.0;
    public double Kurtosis { get; set; } = 3.0;
    public byte?[] Generate(byte basePitch, int length)
    {
        if (length <= 0) return Array.Empty<byte?>();

        var result = new byte?[length];

        // Vorberechnung für Skew-Normal-Standardisierung
        double delta = Skewness / Math.Sqrt(1.0 + Skewness * Skewness); // in [-1,1)
        double meanSN = delta * Math.Sqrt(2.0 / Math.PI);                     // E[X] für Skew-Normal(0,1,alpha)
        double varSN = 1.0 - (2.0 * delta * delta / Math.PI);                // Var[X]
        double stdSN = Math.Sqrt(varSN);

        for (int i = 0; i < length; i++)
        {
            // Zwei unabhängige standardisierte Zufallsvariablen (mit optional heavy tails)
            double z0 = NextStandardized(); // ~ N(0,1) bei excessKurtosis=0, sonst standardisierte t
            double z1 = NextStandardized();

            double x;

            if (Math.Abs(Skewness) < 1e-9)
            {
                // Keine Schiefe -> direkt eine standardisierte Variable
                x = z0;
            }
            else
            {
                // Azzalini-Skew: Kombination aus |z0| und z1
                double comp = delta * Math.Abs(z0) + Math.Sqrt(1.0 - delta * delta) * z1;

                // Standardisieren auf Mittelwert 0, Varianz 1 (für den Skew-Normal-Fall)
                // Hinweis: z0, z1 sind bereits standardisiert (auch bei heavy tails).
                // Die folgende Zentrierung/Skalierung sorgt für sauberes σ-Mapping.
                x = (comp - meanSN) / stdSN;
            }

            // Skalieren auf gewünschte σ und Verschiebung um basePitch
            int pitch = (int)Math.Round(basePitch + StandardDeviation * x);
            result[i] = (byte)Math.Clamp(pitch, 21, 108);
        }

        return result;
    }

    /// <summary>
    /// Liefert eine standardisierte Zufallsvariable mit E=0, Var=1.
    /// - Bei excessKurtosis == 0: Standard-Normal via Box-Muller.
    /// - Bei excessKurtosis  > 0: Standardisierte Student-t mit ν so, dass Excess Kurtosis = 6/(ν-4).
    /// </summary>
    private double NextStandardized()
    {
        if (Kurtosis <= 0.0)
        {
            return NextGaussian01();
        }

        // Ableitung ν aus Excess Kurtosis: κ = 6/(ν-4)  =>  ν = 4 + 6/κ
        // Begrenzen: sehr große κ -> ν -> 4+, numerisch stabil halten
        double nu = 4.0 + 6.0 / Kurtosis;
        if (nu < 4.0001) nu = 4.0001; // Sicherheit

        // Student-t via Z / sqrt(U/nu), Z~N(0,1), U~ChiSq(nu)
        double z = NextGaussian01();
        double u = SampleChiSquare(nu);

        double t = z / Math.Sqrt(u / nu); // t(nu)

        // Standardisieren auf Var=1 (Var(t) = nu/(nu-2) für nu>2)
        double std = Math.Sqrt(nu / (nu - 2.0));
        return t / std;
    }

    /// <summary>Standard-Normal(0,1) per Box–Muller.</summary>
    private double NextGaussian01()
    {
        double u1 = 1.0 - RandomProvider.Get(Seed).NextDouble(); // (0,1]
        double u2 = 1.0 - RandomProvider.Get(Seed).NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }

    /// <summary>Marsaglia–Tsang Gamma-Sampler für alpha &gt; 1.</summary>
    private double SampleGamma(double alpha)
    {
        // Für unseren Anwendungsfall gilt: nu>4 => alpha=nu/2>2 -> OK
        double d = alpha - 1.0 / 3.0;
        double c = 1.0 / Math.Sqrt(9.0 * d);

        while (true)
        {
            double x = NextGaussian01();
            double v = 1.0 + c * x;
            if (v <= 0) continue;

            v = v * v * v;
            double u = RandomProvider.Get(Seed).NextDouble();

            // Akzeptanzbedingung
            if (u < 1.0 - 0.0331 * (x * x) * (x * x)) return d * v;
            if (Math.Log(u) < 0.5 * x * x + d * (1.0 - v + Math.Log(v))) return d * v;
        }
    }

    /// <summary>Chi-Quadrat(nu) = Gamma(k=nu/2, theta=2).</summary>
    private double SampleChiSquare(double nu)
    {
        // Gamma(alpha = nu/2, theta = 2)
        double alpha = nu / 2.0;
        double gammaScale2 = 2.0;
        return SampleGamma(alpha) * gammaScale2;
    }
}