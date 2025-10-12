using Egami.Rhythm.Pattern;

namespace Egami.Rhythm.Generation;

public class LSystemGenerator(
    string axiom,
    IReadOnlyDictionary<char, string> rules,
    int iterations,
    char hitSymbol = 'X') : IRhythmGenerator
{

    private readonly string _axiom = axiom;
    private readonly IReadOnlyDictionary<char, string> _rules = rules;
    private readonly int _it = Math.Min(30, Math.Max(0, iterations)); // Begrenzung der Iterationen, um exponentielles Wachstum zu vermeiden
    private readonly char _hit = hitSymbol;

    public static List<Dictionary<char, string>> Rules = new()
    {
        // Periodisch
        new()
        {
            { 'A', "AB" },
            { 'B', "A" }
        },
        // Alternierend
        new()
        {
            { 'X', "XY" },
            {'Y', "X" }
        },
        // Verdichtend
        new()
        {
            { 'A', "AA" },
            { 'B', "B"}
        },
        // Expandierend
        new()
        {
            { 'A', "AB" },
            { 'B', "BB" }
        }
    };



    public RhythmPattern Generate(RhythmContext ctx)
    {
        string current = _axiom;
        for (int i = 0; i < _it; i++)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var c in current)
                sb.Append(_rules.TryGetValue(c, out var repl) ? repl : c.ToString());
            current = sb.ToString();
        }

        var n = ctx.StepsTotal > 0 ? ctx.StepsTotal : current.Length;
        var p = new RhythmPattern(n);
        for (int i = 0; i < n; i++)
        {
            var c = current[i % current.Length];
            bool hit = c == _hit;
            p.Hits[i] = hit;
            p.Lengths[i] = 1;
            p.Velocities[i] = (byte)(hit ? ctx.DefaultVelocity : 0);
        }
        return p;
    }
}
