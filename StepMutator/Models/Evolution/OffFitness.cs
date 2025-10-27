using System.Windows.Input;
using Prism.Mvvm;
using Syncfusion.Windows.Shared;

namespace StepMutator.Models.Evolution;

public class OffFitness : BindableBase, IFitness
{
    private double _weight = 1.0;
    public double Weight
    {
        get => _weight;
        set => SetProperty(ref _weight, value);
    }

    private bool _favorOn = true;
    public bool FavorOn
    {
        get => _favorOn;
        set => SetProperty(ref _favorOn, value);
    }

    public ICommand ToggleOnOffCommand { get; }

    public OffFitness()
    {
        ToggleOnOffCommand = new DelegateCommand(_ => FavorOn = !FavorOn);
    }

    public double Evaluate(ulong individual)
    {
        var step = new Step(individual);
        if (step.On == FavorOn)
        {
            return 1.0 * _weight;
        }

        return 0.0;
    }
}