using Egami.Rhythm;
using Egami.Rhythm.Core;
using Egami.Rhythm.Generation;
using Egami.Rhythm.Pattern;
using Prism.Mvvm;

namespace EuclidEA.ViewModels;

public abstract class RhythmGeneratorViewModel : BindableBase, IRhythmGeneratorViewModel
{
    protected abstract IRhythmGenerator Generator { get; }

    public abstract string Name { get; }

    protected int _steps = 16;

    public virtual int Steps
    {
        get => _steps;
        set => SetProperty(ref _steps, value);
    }

    public RhythmPattern Generate()
    {
        var context = new RhythmContext
        {
            StepsTotal = _steps,
            DefaultVelocity = 100,
            Meter = new Meter(4, 4),
            Timebase = new Timebase(4),
            TempoBpm = 120.0
        };
        return Generate(context);
    }

    protected virtual RhythmPattern Generate(RhythmContext context)
    {
        return Generator.Generate(context);
    }
}