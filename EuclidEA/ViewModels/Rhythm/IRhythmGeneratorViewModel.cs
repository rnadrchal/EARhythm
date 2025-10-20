using System.Windows.Input;
using Egami.Rhythm.Pattern;

namespace EuclidEA.ViewModels.Rhythm;

public interface IRhythmGeneratorViewModel
{
    string Name { get; }
    Sequence Generate();
}