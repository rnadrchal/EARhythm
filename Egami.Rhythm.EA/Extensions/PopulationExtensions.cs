using System.Reflection.Metadata;
using System.Xml.XPath;
using Egami.Rhythm.Common;
using Egami.Rhythm.EA.Mutation;

namespace Egami.Rhythm.EA.Extensions;

/// <summary>
/// Ergebniscontainer für die zwei besten und das schlechteste Individuum.
/// </summary>
public readonly struct Top2WorstResult<T>
{
    public readonly T Best;
    public readonly T Second;
    public readonly T Worst;
    public readonly double BestFitness;
    public readonly double SecondFitness;
    public readonly double WorstFitness;
    public readonly int BestIndex;
    public readonly int SecondIndex;
    public readonly int WorstIndex;

    public Top2WorstResult(T best, T second, T worst,
        double bestFit, double secondFit, double worstFit,
        int bestIdx, int secondIdx, int worstIdx)
    {
        Best = best; Second = second; Worst = worst;
        BestFitness = bestFit; SecondFitness = secondFit; WorstFitness = worstFit;
        BestIndex = bestIdx; SecondIndex = secondIdx; WorstIndex = worstIdx;
    }
}

public static class PopulationExtensions
{
    /// <summary>
    /// Findet das fitteste Individuum in einer Population in O(n).
    /// higherIsBetter = true: größter Fitnesswert gewinnt.
    /// higherIsBetter = false: kleinster Fitnesswert gewinnt (z. B. bei Distanzmaßen).
    /// </summary>
    public static (T Individual, double Fitness, int Index) FindFittest<T>(
        this IReadOnlyList<T> population,
        Func<T, double> fitnessSelector,
        bool higherIsBetter = true)
    {
        if (population is null) throw new ArgumentNullException(nameof(population));
        if (fitnessSelector is null) throw new ArgumentNullException(nameof(fitnessSelector));
        if (population.Count == 0) throw new InvalidOperationException("Population ist leer.");

        int bestIndex = 0;
        double bestFitness = fitnessSelector(population[0]);

        for (int i = 1; i < population.Count; i++)
        {
            double current = fitnessSelector(population[i]);
            if (double.IsNaN(current)) continue; // NaN ignorieren
            bool isBetter = higherIsBetter ? current > bestFitness : current < bestFitness;
            if (isBetter)
            {
                bestFitness = current;
                bestIndex = i;
            }
        }

        return (population[bestIndex], bestFitness, bestIndex);
    }

    /// <summary>
    /// Findet in einem Durchlauf die zwei fittesten und den am wenigsten fitten Eintrag.
    /// Höhere Fitness ist besser (Standard).
    /// </summary>
    public static Top2WorstResult<T> FindTop2AndWorst<T>(
        this IReadOnlyList<T> population,
        Func<T, double> fitnessSelector,
        bool higherIsBetter = true)
    {
        if (population is null) throw new ArgumentNullException(nameof(population));
        if (fitnessSelector is null) throw new ArgumentNullException(nameof(fitnessSelector));
        int n = population.Count;
        if (n == 0) throw new InvalidOperationException("Population ist leer.");
        if (n == 1)
        {
            var f = fitnessSelector(population[0]);
            return new Top2WorstResult<T>(population[0], population[0], population[0], f, f, f, 0, 0, 0);
        }

        // Vorinitialisierung
        double best1 = double.NegativeInfinity, best2 = double.NegativeInfinity;
        double worst = double.PositiveInfinity;
        int iBest1 = -1, iBest2 = -1, iWorst = -1;

        // Optional: für lower-is-better einfach das Vorzeichen drehen
        double sign = higherIsBetter ? 1.0 : -1.0;

        for (int i = 0; i < n; i++)
        {
            double fRaw = fitnessSelector(population[i]);
            // NaN-handling: behandle NaN als sehr schlecht
            double f = double.IsNaN(fRaw) ? double.NegativeInfinity * sign : fRaw;

            double s = f * sign; // transformierte Fitness (größer = besser)

            // Update best1 / best2
            if (s > best1)
            {
                best2 = best1; iBest2 = iBest1;
                best1 = s; iBest1 = i;
            }
            else if (s > best2)
            {
                best2 = s; iBest2 = i;
            }

            // Update worst (immer auf der Original-Skala)
            if (f < (higherIsBetter ? worst : -worst))
            {
                // Für lower-is-better interpretieren wir "worst" als größter f
                if (higherIsBetter) { worst = f; iWorst = i; }
                else { worst = -f; iWorst = i; }
            }
        }

        if (iBest2 == -1) { iBest2 = iBest1; best2 = best1; } // n==2 Edgecase oder alle gleich

        var best = population[iBest1];
        var second = population[iBest2];
        var worstItem = population[iWorst];

        // Fitnesswerte auf Original-Skala zurückrechnen
        double bestFit = higherIsBetter ? best1 : best1 * (1.0 / sign);
        double secondFit = higherIsBetter ? best2 : best2 * (1.0 / sign);
        double worstFit = fitnessSelector(worstItem);

        return new Top2WorstResult<T>(
            best, second, worstItem,
            bestFit, secondFit, worstFit,
            iBest1, iBest2, iWorst
        );
    }

    /// <summary>
    /// Komfort-Overload, wenn die Fitness bereits als Eigenschaft vorliegt.
    /// </summary>
    public static Top2WorstResult<T> FindTop2AndWorst<T>(
        this IReadOnlyList<T> population,
        Func<T, double> fitnessPropertyGetter) =>
        population.FindTop2AndWorst(fitnessPropertyGetter, higherIsBetter: true);

}