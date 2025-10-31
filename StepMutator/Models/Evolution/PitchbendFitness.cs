using System;
using Prism.Mvvm;

namespace StepMutator.Models.Evolution;

public class PitchbendFitness : BindableBase, IFitness
{
    private double _weight = 0.1;
    public double Weight
    {
        get => _weight;
        set => SetProperty(ref _weight, value);
    }

    private ushort _targetPitchbend = 2048;
    public ushort TargetPitchbend
    {
        get => _targetPitchbend;
        set => SetProperty(ref _targetPitchbend, value);
    }

    public double Cents
    {
        get => PitchbendHelpers.RawToCents(_targetPitchbend, 0.5);
        set => TargetPitchbend = (ushort)PitchbendHelpers.CentsToRaw(value, 0.5);
    }

    public double Evaluate(ulong individual)
    {
        var step = new Step(individual);
        var diff = Math.Abs(step.Pitchbend - _targetPitchbend);
        var score = 1.0 - (double)diff / 4096.0;
        return score * _weight;
    }
}