using System;
using Prism.Mvvm;

namespace StepMutator.Models.Evolution;

public class NoteFitness : BindableBase, IFitness
{
    private double _weight = 1.0;
    public double Weight
    {
        get => _weight;
        set => SetProperty(ref _weight, value);
    }

    private byte _targetNote = 60;
    public byte TargetNote
    {
        get => _targetNote;
        set => SetProperty(ref _targetNote, value);
    }

    public double Evaluate(ulong individual)
    {
        var step = new Step(individual);
        var result = Math.Max(0.0, 1.0 - Math.Abs(_targetNote - step.Pitch) / 127.0);
        return result * _weight;
    }
}