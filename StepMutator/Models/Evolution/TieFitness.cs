using System.Windows.Input;
using Prism.Mvvm;

namespace StepMutator.Models.Evolution;

public class TieFitness : BindableBase, IFitness
{
    private readonly bool _tie;
    private double _weight = 1.0;

    public double Weight
    {
        get => _weight;
        set => SetProperty(ref _weight, value);
    }

    public TieFitness(bool tie)
    {
        _tie = tie;
    }
    public double Evaluate(ulong individual)
    {
        var step = new Step(individual);
        return step.Tie == _tie ? 0.8 * _weight : 0.2 * _weight;
    }
}