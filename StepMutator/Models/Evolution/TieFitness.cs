using System.Windows.Input;
using Prism.Mvvm;

namespace StepMutator.Models.Evolution;

public class TieFitness : BindableBase, IFitness
{
    private double _weight = 1.0;

    public double Weight
    {
        get => _weight;
        set => SetProperty(ref _weight, value);
    }

    private bool _legato = true;
    public bool Legato
    {
        get => _legato;
        set => SetProperty(ref _legato, value);
    }

    public ICommand ToggleTieCommand { get; }

    public TieFitness()
    {
        ToggleTieCommand = new Prism.Commands.DelegateCommand(() => Legato = !Legato);
    }
    public double Evaluate(ulong individual)
    {
        var step = new Step(individual);
        return step.Tie == _legato ? 0.8 : 0.2;
    }
}