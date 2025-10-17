using System.Collections.Generic;
using System.Windows.Documents;
using Egami.Pitch;
using Egami.Rhythm;
using Egami.Rhythm.Core;
using Egami.Rhythm.Extensions;
using Egami.Rhythm.Generation;
using Egami.Rhythm.Midi.Generation;
using Egami.Rhythm.Pattern;
using EuclidEA.ViewModels.Pitch;
using Prism.Mvvm;

namespace EuclidEA.ViewModels;

public abstract class RhythmGeneratorViewModel : BindableBase, IRhythmGeneratorViewModel
{
    protected abstract IRhythmGenerator Generator { get; }

    public abstract string Name { get; }

    protected List<IPitchGeneratorViewModel> PitchGenerators { get; } = new()
    {
        new ConstantPitchGeneratorViewModel(new ConstantPitchGenerator()),
        new RandomPitchGeneratorViewModel(new RandomPitchGenerator()),
        new NormalDistributionPitchGeneratorViewModel(new NormalDistributionPitchGenerator()),
        new RecordPitchGeneratorViewModel(new RecordPitchGenerator())
    };

    private int? _pitchGeneratorIndex = 0;
    public int? PitchGeneratorIndex
    {
        get => _pitchGeneratorIndex;
        set
        {
            if (SetProperty(ref _pitchGeneratorIndex, value))
            {
                RaisePropertyChanged(nameof(PitchGenerator));
            }
        }
    }

    public virtual IPitchGeneratorViewModel PitchGenerator => _pitchGeneratorIndex.HasValue ? PitchGenerators[_pitchGeneratorIndex.Value] : null;

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
        if (PitchGenerator != null)
        {
            var pitches = PitchGenerator.Generate(context.StepsTotal);
            return Generator.GenerateWith(context, new PitchTransform(pitches));
        }
        return Generator.Generate(context);
    }
}