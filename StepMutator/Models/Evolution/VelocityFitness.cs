using System;
using Prism.Mvvm;

namespace StepMutator.Models.Evolution;

public class VelocityFitness : BindableBase, IFitness
{
    private double _weight = 1.0;
    public double Weight
    {
        get => _weight;
        set => SetProperty(ref _weight, value);
    }

    private byte _targetVelocity = 100;
    public byte TargetVelocity
    {
        get => _targetVelocity;
        set => SetProperty(ref _targetVelocity, value);
    }
    public double Evaluate(ulong individual)
    {
        var step = new Step(individual);
        var result = Math.Max(0.0, 1.0 - Math.Abs(_targetVelocity - step.Velocity) / 127.0);
        return result * _weight;
    }
}