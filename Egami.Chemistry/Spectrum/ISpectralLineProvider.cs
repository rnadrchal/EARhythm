namespace Egami.Chemistry.Spectrum;


public readonly record struct SpectralLine(
    double WavelengthNm,
    double Intensity,
    double OpticalFrequencyHz  // aus WavelengthNm berechnet
);

public interface ISpectralLineProvider
{
    IEnumerable<SpectralLine> GetDominentSpectralLines(string elementName);
}