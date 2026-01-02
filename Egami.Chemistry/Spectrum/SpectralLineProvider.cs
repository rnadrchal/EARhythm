namespace Egami.Chemistry.Spectrum;

public sealed class SpectralLineProvider : ISpectralLineProvider
{
    private List<ElementTop3Lines> _dominantSpectralLines;

    public SpectralLineProvider()
    {
        _dominantSpectralLines = ElementTop3LinesCsvLoader.Load("element_top3_visible_lines.csv").ToList();
    }

    public IEnumerable<SpectralLine> GetDominentSpectralLines(string elementName)
    {
        var line = _dominantSpectralLines.SingleOrDefault(l => l.Symbol == elementName);
        if (line is null)
            throw new KeyNotFoundException(elementName);
        if (line.Line1.HasValue) 
            yield return line.Line1.Value;
        if (line.Line2.HasValue)
            yield return line.Line2.Value;
        if (line.Line3.HasValue)
            yield return line.Line3.Value;
    }
}