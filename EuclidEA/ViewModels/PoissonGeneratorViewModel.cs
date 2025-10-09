using Egami.Rhythm.Generation;

namespace EuclidEA.ViewModels;

public sealed class PoissonGeneratorViewModel : RhythmGeneratorViewModel
{
    private double _lambdaPerBar = 4.0;

    public double LambdaPerBar
    {
        get => _lambdaPerBar;
        set => SetProperty(ref _lambdaPerBar, value);
    }

    protected override IRhythmGenerator Generator => new PoissonGenerator(_lambdaPerBar);
    public override string Name => "Poisson Generator";
}