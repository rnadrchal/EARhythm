namespace Egami.Chemistry.Services;

public sealed class DefaultElementPropertyProvider : IElementPropertyProvider
{
    private static readonly Dictionary<string, ElementProps> Map = new(StringComparer.Ordinal)
    {
        ["H"] = new(1, 1.008, 2.20),
        ["C"] = new(6, 12.011, 2.55),
        ["N"] = new(7, 14.007, 3.04),
        ["O"] = new(8, 15.999, 3.44),
        ["F"] = new(9, 18.998, 3.98),
        ["P"] = new(15, 30.974, 2.19),
        ["S"] = new(16, 32.06, 2.58),
        ["Cl"] = new(17, 35.45, 3.16),
        ["Br"] = new(35, 79.904, 2.96),
        ["I"] = new(53, 126.904, 2.66),
    };

    public ElementProps? TryGet(string symbol)
        => Map.TryGetValue(symbol, out var v) ? v : null;
}