using Egami.Rhythm.Generation;
using Prism.Mvvm;

namespace EuclidEA.Models;

public class RhythmGeneratorModel : BindableBase
{
    private readonly IRhythmGenerator _rhythmGenerator;

    private string _name;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public RhythmGeneratorModel(IRhythmGenerator rhythmGenerator, string name)
    {
        _rhythmGenerator = rhythmGenerator;
        _name = name;
    }
}