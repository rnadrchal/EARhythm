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

    private double _insertionRate = 0.1;
    public double InsertionRate
    {
        get => _insertionRate;
        set => SetProperty(ref _insertionRate, value);
    }

    private double _swapRate = 0.1;
    public double SwapRate
    {
        get => _swapRate;
        set => SetProperty(ref _swapRate, value);
    }

    private double _inversionRate = 0.1;
    public double InversionRate
    {
        get => _inversionRate;
        set => SetProperty(ref _inversionRate, value);
    }

    private double _transpositionRate = 0.1;

    public double TranspositionRate
    {
        get => _transpositionRate;
        set => SetProperty(ref _transpositionRate, value);
    }

    private double _crossoverRate = 0.7;
    public double CrossoverRate
    {
        get => _crossoverRate;
        set => SetProperty(ref _crossoverRate, value);
    }

}