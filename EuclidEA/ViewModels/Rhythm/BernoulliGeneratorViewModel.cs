using Egami.Rhythm.Generation;

namespace EuclidEA.ViewModels.Rhythm;

public sealed class BernoulliGeneratorViewModel : RhythmGeneratorViewModel
{
    private double _probability01 = 0.5;

    public double Probability01
    {
        get => _probability01;
        set => SetProperty(ref _probability01, value);
    }

    

    protected override IRhythmGenerator Generator => new BernoulliGenerator(_probability01);
    public override string Name => "Bernoulli Generator";
}