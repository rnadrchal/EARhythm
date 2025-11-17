using System.Windows.Input;
using Prism.Mvvm;
using Syncfusion.Windows.Shared;

namespace StepMutator.Models.Evolution;

public class HitFitness : BindableBase, IFitness
{
    private readonly bool _hit;
    private double _weight = 1.0;

    public HitFitness(bool hit)
    {
        _hit = hit;
    }

    public double Weight
    {
        get => _weight;
        set => SetProperty(ref _weight, value);
    }


    public double Evaluate(ulong individual)
    {
        var step = new Step(individual);
        return step.On == _hit ? 0.99 * _weight : 0.1 * _weight;
    }
}