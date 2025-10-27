using System;
using Prism.Mvvm;

namespace StepMutator.Models.Evolution;

public class PitchbendFitness : BindableBase, IFitness
{
    private double _weight = 0.2;
    public double Weight
    {
        get => _weight;
        set => SetProperty(ref _weight, value);
    }

    private ushort _targetPitchbend = 8192;
    public ushort TargetPitchbend
    {
        get => _targetPitchbend;
        set => SetProperty(ref _targetPitchbend, value);
    }

    public double Cents
    {
        get => (_targetPitchbend - 8192)  / 8192.0 * 50.0;
        set
        {
            _targetPitchbend = (ushort)(8192 + (value / 50.0 * 8192.0));
            RaisePropertyChanged(nameof(TargetPitchbend));
        }
    }

    public double Evaluate(ulong individual)
    {
        var step = new Step(individual);
        var diff = Math.Abs(step.Pitchbend - _targetPitchbend);
        var score = 1.0 - (double)diff / 16383.0;
        return score * _weight;
    }
}