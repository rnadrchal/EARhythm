using Egami.Rhythm;
using Egami.Rhythm.Generation;
using Egami.Rhythm.Pattern;
using Egami.Rhythm.Transformation;

namespace EuclidEA.ViewModels;

public sealed class EuclidGeneratorViewModel : RhythmGeneratorViewModel
{
    public override string Name => "Euclid Generator";

    public override int Steps
    {
        get => _steps;
        set
        {
            if (SetProperty(ref _steps, value))
            {
                if (_pulses > _steps) Pulses = _steps;
            }
        }
    }

    private int _pulses = 4;

    public int Pulses
    {
        get => _pulses;
        set
        {
            if (value <= _steps) SetProperty(ref _pulses, value);
        }
    }

    private int _rotation = 0;

    public int Rotation
    {
        get => _rotation;
        set => SetProperty(ref _rotation, value);
    }

    protected override IRhythmGenerator Generator => new EuclidGenerator(_pulses, _rotation);

    protected override RhythmPattern Generate(RhythmContext context)
    {
        var rotate = new RotateTransform(_rotation);
        return rotate.Apply(context, base.Generate(context));
    }
}