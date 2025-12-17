namespace Egami.Sequencer.Pcs;

public sealed class PitchClassSet : IEquatable<PitchClassSet>
{
    // Immer sortiert, ohne Duplikate, Werte 0..11
    private readonly int[] _pitchClasses;

    public IReadOnlyList<int> PitchClasses => _pitchClasses;

    public PitchClassSet(IEnumerable<int> pitchClasses)
    {
        var normalized = pitchClasses
            .Select(pc => ((pc % 12) + 12) % 12) // modulo 12, auch für negative Werte
            .Distinct()
            .OrderBy(pc => pc)
            .ToArray();

        if (normalized.Length == 0)
            throw new ArgumentException("PitchClassSet darf nicht leer sein.", nameof(pitchClasses));

        _pitchClasses = normalized;
    }

    public static PitchClassSet FromMidiNotes(IEnumerable<int> midiNotes)
        => new(midiNotes.Select(n => n % 12));

    public int Cardinality => _pitchClasses.Length;

    public PitchClassSet Transpose(int semitones)
        => new(_pitchClasses.Select(pc => pc + semitones));

    public bool ContainsPitchClass(int pitchClass)
    {
        var pc = ((pitchClass % 12) + 12) % 12;
        return Array.BinarySearch(_pitchClasses, pc) >= 0;
    }

    public override string ToString()
        => "{" + string.Join(",", _pitchClasses) + "}";

    #region Equality

    public bool Equals(PitchClassSet? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (_pitchClasses.Length != other._pitchClasses.Length) return false;
        for (int i = 0; i < _pitchClasses.Length; i++)
        {
            if (_pitchClasses[i] != other._pitchClasses[i]) return false;
        }
        return true;
    }

    public override bool Equals(object? obj)
        => Equals(obj as PitchClassSet);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            foreach (var pc in _pitchClasses)
                hash = hash * 31 + pc;
            return hash;
        }
    }

    #endregion

    #region Prime Form (vereinfachte Variante)

    /// <summary>
    /// Liefert eine normalisierte Prime Form (nur Tn/TnI-Äquivalenz, keine Forte-Nummern).
    /// </summary>
    public PitchClassSet GetPrimeForm()
    {
        // 1. Alle Rotationen der Normalform
        var rotations = GetRotations(_pitchClasses);

        // 2. Spannen (Span) berechnen, kleinsten wählen
        var best = rotations
            .Select(r => new { Set = r, Span = GetSpan(r) })
            .OrderBy(x => x.Span)
            .ThenBy(x => LexicographicValue(x.Set))
            .First()
            .Set;

        // 3. Inversion
        var inverted = best.Select(pc => (12 - pc) % 12).ToArray();
        Array.Sort(inverted);

        var prime = LexicographicLessOrEqual(best, inverted) ? best : inverted;

        return new PitchClassSet(prime);
    }

    private static IEnumerable<int[]> GetRotations(int[] pcs)
    {
        for (int i = 0; i < pcs.Length; i++)
        {
            var rotated = new int[pcs.Length];
            for (int j = 0; j < pcs.Length; j++)
                rotated[j] = (pcs[(i + j) % pcs.Length] - pcs[i] + 12) % 12;

            Array.Sort(rotated);
            yield return rotated;
        }
    }

    private static int GetSpan(int[] pcs)
        => pcs[^1] - pcs[0];

    private static int LexicographicValue(int[] pcs)
    {
        int value = 0;
        for (int i = 0; i < pcs.Length; i++)
            value = value * 12 + pcs[i];
        return value;
    }

    private static bool LexicographicLessOrEqual(int[] a, int[] b)
    {
        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] < b[i]) return true;
            if (a[i] > b[i]) return false;
        }
        return true;
    }

    #endregion
}