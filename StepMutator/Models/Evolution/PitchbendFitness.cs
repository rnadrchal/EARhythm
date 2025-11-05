using System;
using Prism.Mvvm;

namespace StepMutator.Models.Evolution;

public class PitchbendFitness : BindableBase, IFitness
{
    private readonly ushort _pitchbend;
    private double _weight = 0.1;

    public PitchbendFitness(ushort pitchbend)
    {
        _pitchbend = pitchbend;
    }

    public double Weight
    {
        get => _weight;
        set => SetProperty(ref _weight, value);
    }

    public double Evaluate(ulong individual)
    {
        var step = new Step(individual);
        var diff = Math.Abs(step.Pitchbend - _pitchbend);
        var score = 1.0 - (double)diff / 4096.0;
        return score * _weight;
    }
}