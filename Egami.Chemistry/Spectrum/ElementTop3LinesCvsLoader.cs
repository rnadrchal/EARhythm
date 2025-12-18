using System.Globalization;

namespace Egami.Chemistry.Spectrum;

internal sealed record ElementTop3Lines(
    int Z,
    string Symbol,
    string Name,
    SpectralLine? Line1,
    SpectralLine? Line2,
    SpectralLine? Line3
);

internal static class ElementTop3LinesCsvLoader
{
    // Lichtgeschwindigkeit (m/s)
    private const double C = 299_792_458.0;

    public static IReadOnlyList<ElementTop3Lines> Load(string csvPath)
    {
        using var reader = new StreamReader(csvPath);
        var header = reader.ReadLine();
        if (header is null)
            throw new InvalidOperationException("CSV is empty.");

        var result = new List<ElementTop3Lines>();
        var inv = CultureInfo.InvariantCulture;

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var cols = SplitCsvLine(line);
            // Erwartete Spalten:
            // Z,Symbol,Name,Lambda1_nm,I1,Lambda2_nm,I2,Lambda3_nm,I3
            if (cols.Count < 9)
                throw new FormatException($"Invalid CSV row: {line}");

            var z = int.Parse(cols[0], inv);
            var symbol = cols[1];
            var name = cols[2];

            var l1 = ParseLine(cols[3], cols[4], inv);
            var l2 = ParseLine(cols[5], cols[6], inv);
            var l3 = ParseLine(cols[7], cols[8], inv);

            result.Add(new ElementTop3Lines(z, symbol, name, l1, l2, l3));
        }

        return result;

        static SpectralLine? ParseLine(string wavelengthNm, string intensity, IFormatProvider inv)
        {
            if (string.IsNullOrWhiteSpace(wavelengthNm) || string.IsNullOrWhiteSpace(intensity))
                return null;
            var lambdaNm = double.Parse(wavelengthNm, inv);
            var I = double.Parse(intensity, inv);

            // λ[nm] -> λ[m]
            var lambdaM = lambdaNm * 1e-9;
            var freqHz = C / lambdaM;

            return new SpectralLine(lambdaNm, I, freqHz);
        }
    }

    // Minimaler CSV-Splitter (für deine Datei ausreichend: keine Quotes/Kommas in Feldern erwartet)
    private static List<string> SplitCsvLine(string line)
    {
        return line.Split(',').Select(s => s.Trim()).ToList();
    }
}
