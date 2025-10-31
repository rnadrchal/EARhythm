using Prism.Mvvm;
using System;

namespace StepMutator.Models.Evolution;

public class ControlChangeFitness : BindableBase, IFitness
{
    private double _weight = 0.1;
    public double Weight
    {
        get => _weight;
        set => SetProperty(ref _weight, value);
    }

    private ushort _targetValue = 127;
    public ushort TargetValue
    {
        get => _targetValue;
        set => SetProperty(ref _targetValue, value);
    }

    public double Evaluate(ulong individual)
    {
        var step = new Step(individual);
        var result = Math.Max(0.0, 1.0 - Math.Abs(_targetValue - step.ModWheel) / 127.0);
        return result * _weight;
    }
}