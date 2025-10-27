using Prism.Mvvm;
using StepMutator.Services;

namespace StepMutator.Models.Evolution;

public class EvolutionOptions : BindableBase, IEvolutionOptions
{
    public int? Seed => null;

    private int _populationSize = 50;
    public int PopulationSize
    {
        get => _populationSize;
        set => SetProperty(ref _populationSize, value);
    }

    private int _generationLength = 100;
    public int GenerationLength
    {
        get => _generationLength;
        set => SetProperty(ref _generationLength, value);
    }

    private double _deletionRate = 0.1;
    public double DeletionRate
    {
        get => _deletionRate;
        set => SetProperty(ref _deletionRate, value);
    }

    public double DeletionPercent
    {
        get => _deletionRate * 100.0;
        set
        {
            _deletionRate = value / 100.0;
            RaisePropertyChanged(nameof(DeletionRate));
        }
    }

    private double _insertionRate = 0.1;
    public double InsertionRate
    {
        get => _insertionRate;
        set => SetProperty(ref _insertionRate, value);
    }

    public double InsertionPercent
    {
        get => _insertionRate * 100.0;
        set
        {
            _insertionRate = value / 100.0;
            RaisePropertyChanged(nameof(InsertionRate));
        }
    }

    private double _swapRate = 0.1;
    public double SwapRate
    {
        get => _swapRate;
        set => SetProperty(ref _swapRate, value);
    }

    public double SwapPercent
    {
        get => _swapRate * 100.0;
        set
        {
            _swapRate = value / 100.0;
            RaisePropertyChanged(nameof(SwapRate));
        }
    }

    private double _inversionRate = 0.1;
    public double InversionRate
    {
        get => _inversionRate;
        set => SetProperty(ref _inversionRate, value);
    }

    public double InversionPercent
    {
        get => _insertionRate * 100.0;
        set
        {
            _inversionRate = value / 100.0;
            RaisePropertyChanged(nameof(InversionRate));
        }
    }

    private double _transpositionRate = 0.1;

    public double TranspositionRate
    {
        get => _transpositionRate;
        set => SetProperty(ref _transpositionRate, value);
    }

    public double TranspositionPercent
    {
        get => _transpositionRate * 100.0;
        set
        {
            _transpositionRate = value / 100.0;
            RaisePropertyChanged(nameof(TranspositionRate));
        }
    }

    private double _crossoverRate = 0.7;
    public double CrossoverRate
    {
        get => _crossoverRate;
        set => SetProperty(ref _crossoverRate, value);
    }

    public double CrossoverPercent
    {
        get => _crossoverRate * 100.0;
        set
        {
            _crossoverRate = value / 100.0;
            RaisePropertyChanged(nameof(CrossoverRate));
        }
    }

}