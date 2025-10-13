using Egami.Pitch;

namespace EuclidEA.ViewModels;

public class RandomPitchGeneratorViewModel : PitchGeneratorViewModel
{
    private readonly RandomPitchGenerator _randomPitchGenerator;

    private int _octaves = 1;
    public int Octaves
    {
        get => _octaves;
        set
        {
            if (SetProperty(ref _octaves, value))
            {
                _randomPitchGenerator.Octaves = value;
            }
        }
    }

    public RandomPitchGeneratorViewModel(IPitchGenerator generator) : base(generator)
    {
        _randomPitchGenerator = (RandomPitchGenerator)generator;
    }

    public override string Name => "Random";
}