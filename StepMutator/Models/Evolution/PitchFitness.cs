using System;
using Prism.Mvvm;

namespace StepMutator.Models.Evolution;

public class PitchFitness : BindableBase, IFitness
{
    private readonly byte _pitch;
    private double _weight = 1.0;
    public double Weight
    {
        get => _weight;
        set => SetProperty(ref _weight, value);
    }

    public PitchFitness(byte pitch)
    {
        _pitch = pitch;
    }

    public double Evaluate(ulong individual)
    {
        var step = new Step(individual);
        var result = Math.Max(0.0, 1.0 - Math.Abs(_pitch - step.Pitch) / 127.0);
        return result * _weight;
    }
}