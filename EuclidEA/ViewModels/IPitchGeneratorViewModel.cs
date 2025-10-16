using Egami.Pitch;
using Egami.Rhythm.Pattern;

namespace EuclidEA.ViewModels;

public interface IPitchGeneratorViewModel
{
    string Name { get; }
    byte?[] Generate(int length);
}