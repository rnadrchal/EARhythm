using System.Windows.Input;
using Egami.Rhythm.Pattern;

namespace EuclidEA.ViewModels;

public interface IRhythmGeneratorViewModel
{
    string Name { get; }
    RhythmPattern Generate();
}