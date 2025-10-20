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
        // Periodisch, Fibonacci
        new()
        {
            { 'A', "AB" },
            { 'B', "A" }
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
        },
        // Puls Cluster
        new ()
        {
            { 'A', "ABB" },
            { 'B', "A"}
        },
        // Symmetrisch
        new ()
        {
            { 'A', "ABBA" },
            { 'B', "BAAB"}
        },
        // Chaotisch/Organisch
        new ()
        {
            { 'A', "AB" },
            { 'B', "BA"}
        },
        // Fraktal reduziert
        new ()
        {
            { 'A', "ABA" },
            { 'B', "B"}
        },
        // Binär rhytmisch
        new ()
        {
            { 'A', "ABAC" },
            { 'B', "B"},
            { 'C', "C" }
        },
        // Polymetrisch
        new ()
        {
            { 'A', "AB" },
            { 'B', "AC"},
            { 'C', "A" }
        },
    };



    public Sequence Generate(RhythmContext ctx)
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
        var p = new Sequence(n);
        for (int i = 0; i < n; i++)
        {
            var c = current[i % current.Length];
            bool hit = c == _hit;
            p.Steps[i].Hit = hit;
            p.Steps[i].Length = 1;
            p.Steps[i].Velocity = (byte)(hit ? ctx.DefaultVelocity : 0);

        }
        return p;
    }
}
