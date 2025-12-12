using System.Security.Cryptography;

namespace Egami.Sequence.Tests;

[TestClass]
public class WortKlauBeReiTests
{
    private const string Text = "WORT-KLAU-BE-REI";

    [TestMethod]
    public void CanCreateParts()
    {
        var parts = Parts;
    }

    [TestMethod]
    public void CanGetRawText()
    {
        var rawText = RawText;
    }

    [TestMethod]
    public void CanGetIndices()
    {
        var indices = Indices;
        var charOnlyIncices = CharOnlyIndices;
    }

    [TestMethod]
    public void CanCalculateQuersummen()
    {
        var quersummen = Quersummen();
        var quersumme = Quersumme();
    }

    [TestMethod]
    public void CanGetMean()
    {
        var mean = Mean();
    }

    private string[] Parts = Text.Split('-');

    private string RawText = Text.Replace("-", "");

    int[] Indices => Text.Select(t => t is >= 'A' and <= 'Z' ? t - 'A' : -1).ToArray();

    int[] CharOnlyIndices => Indices.Where(i => i >= 0).ToArray();

    int[] Quersummen()
    {
        return Parts.Select(p => p.Sum(c => c - 'A')).ToArray();
    }

    int Quersumme()
    {
        return RawText.Sum(c => c - 'A');
    }

    double Mean()
    {
        return Quersumme() / (double)CharOnlyIndices.Length;
    }
}