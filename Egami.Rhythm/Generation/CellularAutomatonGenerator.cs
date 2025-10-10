using Egami.Rhythm.Common;
using Egami.Rhythm.Pattern;

namespace Egami.Rhythm.Generation;

public enum CaRule
{
    Rule30 = 30,
    Rule45 = 45,
    Rule54 = 54,
    Rule60 = 60,
    Rule90 = 90,
    Rule102 = 102,
    Rule110 = 110, // Turing-voll, „komplex“
    Rule126 = 126,
    Rule150 = 150, // XOR-artig
    Rule184 = 184  // „Traffic rule“
}

public enum CaBoundary
{
    Wrap,      // zirkulär (Torus)
    Fixed0, // außen immer 0
    Fixed1   // außen immer 1
}

public enum CaSeed
{
    Center,   // eine 1 in der Mitte
    Random          // vom Aufrufer via ctor übergeben
}

public enum CaMapMode
{
    Last,     // nur letzte Generation → Pattern
    Any,      // OR über alle Generationen
    Sum,     // Summe der Hits pro Zelle, auf Velocity gemappt (clip 1..127)
    N       // jede N-te Generation als Schritt (temporal flatten)
}

/// <summary>
/// 1D-Elementar-CA (Wolfram) → RhythmPattern.
/// Unterstützt mehrere Regeln, Randbedingungen, Seeds und Mapping-Modi.
/// </summary>
public class CellularAutomatonGenerator(
    int generations = 1,
    CaRule rule = CaRule.Rule90,
    CaBoundary boundary = CaBoundary.Wrap,
    CaSeed seedMode = CaSeed.Center,
    CaMapMode mapMode = CaMapMode.Last,
    int everyN = 2,
    bool[]? customSeed = null) : IRhythmGenerator
{
    private readonly int _generations = generations;
    private readonly CaRule _rule = rule;
    private readonly CaBoundary _boundary = boundary;
    private readonly CaSeed _seedMode = seedMode;
    private readonly CaMapMode _mapMode = mapMode;
    private readonly int _everyN = everyN;
    private readonly bool[]? _customSeed = customSeed;

    public RhythmPattern Generate(RhythmContext ctx)
    {
        int n = ctx.StepsTotal;

        // 1) Startzeile
        var row = SeedRow(ctx);
        // Puffer für Historie (optional)
        var history = new List<bool[]>(_generations) { (bool[])row.Clone() };

        // 2) Iteriere Generationen
        byte ruleMask = (byte)_rule;
        for (int g = 1; g < _generations; g++)
        {
            row = Step(row, ruleMask, _boundary);
            history.Add((bool[])row.Clone());
        }

        // 3) Mapping → RhythmPattern
        var p = new RhythmPattern(n);

        switch (_mapMode)
        {
            case CaMapMode.Last:
                {
                    var last = history[^1];
                    for (int i = 0; i < n; i++)
                    {
                        bool hit = last[i % ctx.StepsTotal];
                        p.Hits[i] = hit;
                        p.Velocity[i] = hit ? ctx.DefaultVelocity : (byte)0;
                        p.Lengths[i] = hit ? 1 : 0;
                    }
                    break;
                }

            case CaMapMode.Any:
                {
                    // OR über alle Generationen an Position i%width
                    for (int i = 0; i < n; i++)
                    {
                        int x = i % ctx.StepsTotal;
                        bool hit = false;
                        foreach (var gen in history)
                            if (gen[x]) { hit = true; break; }

                        p.Hits[i] = hit;
                        p.Velocity[i] = hit ? ctx.DefaultVelocity : (byte)0;
                        p.Lengths[i] = hit ? 1 : 0;
                    }
                    break;
                }

            case CaMapMode.Sum:
                {
                    for (int i = 0; i < n; i++)
                    {
                        int x = i % ctx.StepsTotal;
                        int sum = 0;
                        foreach (var gen in history)
                            if (gen[x]) sum++;

                        if (sum > 0)
                        {
                            p.Hits[i] = true;
                            // Mappe Summe auf 1..127 (linear, capped)
                            int vel = 1 + (int)Math.Round(126.0 * sum / _generations);
                            p.Velocity[i] = (byte)Math.Clamp(vel, 1, 127);
                            p.Lengths[i] = 1;
                        }
                    }
                    break;
                }

            case CaMapMode.N:
                {
                    // Falte die Zeitdimension: jede N-te Generation erzeugt der Reihe nach Schritte.
                    // Effektiv entstehen (_generations/_everyN)*_width Schritte; wir samplen modulo auf n.
                    int stepIdx = 0;
                    for (int g = 0; g < history.Count; g += _everyN)
                    {
                        var gen = history[g];
                        for (int x = 0; x < ctx.StepsTotal; x++)
                        {
                            bool hit = gen[x];
                            int i = stepIdx % n;
                            if (hit)
                            {
                                p.Hits[i] = true;
                                p.Velocity[i] = ctx.DefaultVelocity;
                                p.Lengths[i] = 1;
                            }
                            stepIdx++;
                        }
                    }
                    break;
                }
        }

        return p;
    }

    // --- Helpers -------------------------------------------------------------

    private bool[] SeedRow(RhythmContext ctx)
    {
        var row = new bool[ctx.StepsTotal];

        switch (_seedMode)
        {
            case CaSeed.Center:
                row[ctx.StepsTotal / 2] = true;
                break;

            case CaSeed.Random:
                {
                    var rng = RandomProvider.Get(ctx.Seed); // Seed-aware
                    for (int i = 0; i < ctx.StepsTotal; i++)
                        row[i] = rng.Next(2) == 1;
                    break;
                }
        }
        return row;
    }

    private static bool[] Step(bool[] cur, byte ruleMask, CaBoundary boundary)
    {
        int n = cur.Length;
        var next = new bool[n];

        for (int i = 0; i < n; i++)
        {
            bool left, center = cur[i], right;

            // Nachbarschaft bestimmen
            int li = i - 1, ri = i + 1;

            left = boundary switch
            {
                CaBoundary.Wrap => cur[(li + n) % n],
                CaBoundary.Fixed0 => li >= 0 && cur[li],
                CaBoundary.Fixed1 => li < 0 || cur[li],
                _ => cur[(li + n) % n]
            };

            right = boundary switch
            {
                CaBoundary.Wrap => cur[ri % n],
                CaBoundary.Fixed0 => ri < n && cur[ri],
                CaBoundary.Fixed1 => ri >= n || cur[ri],
                _ => cur[ri % n]
            };

            // Neighborhood als 3-Bit-Code: L C R → 4..0
            int code = (left ? 4 : 0) | (center ? 2 : 0) | (right ? 1 : 0);
            // Wolfram: Bit an Position 'code' (0..7) entscheidet
            bool bit = ((ruleMask >> code) & 0b1) == 1;
            next[i] = bit;
        }

        return next;
    }
}