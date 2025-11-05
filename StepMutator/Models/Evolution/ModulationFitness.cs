using Prism.Mvvm;
using System;

namespace StepMutator.Models.Evolution;

public class ModulationFitness : BindableBase, IFitness
{
    private readonly byte _value;
    private double _weight = 0.1;
    public double Weight
    {
        get => _weight;
        set => SetProperty(ref _weight, value);
    }

    public ModulationFitness(byte value)
    {
        _value = value;
    }

    public double Evaluate(ulong individual)
    {
        var step = new Step(individual);
        var result = Math.Max(0.0, 1.0 - Math.Abs(_value - step.ModWheel) / 127.0);
        return result * _weight;
    }
}