using Egami.Rhythm.EA;
using Prism.Mvvm;

namespace EuclidEA.Models;

public class EvolutionOptions : BindableBase, IEvolutionOptions
{
    private int _populationSize = 8;
    private double _deletionRate = 0.01;
    private double _insertionRate = 0.01;
    private double _inversionRate = 0.01;
    private double _transpositionRate = 0.01;
    private double _retrogradeRate = 0.01;
    private double _melodicInversionRate = 0.01;
    private double _swapRate = 0.05;
    private double _crossoverRate = 0.7;
    private int _maxStepLength = 4;
    private int? _seed;

    public int PopulationSize
    {
        get => _populationSize;
        set => SetProperty(ref _populationSize, value);
    }

    public double DeletionRate
    {
        get => _deletionRate;
        set => SetProperty(ref _deletionRate, value);
    }

    public double InsertionRate
    {
        get => _insertionRate;
        set => SetProperty(ref _insertionRate, value);
    }

    public double InversionRate
    {
        get => _inversionRate;
        set => SetProperty(ref _inversionRate, value);
    }

    public double TranspositionRate
    {
        get => _transpositionRate;
        set => SetProperty(ref _transpositionRate, value);
    }

    public double RetrogradeRate
    {
        get => _retrogradeRate;
        set => SetProperty(ref _retrogradeRate, value);
    }

    public double MelodicInversionRate
    {
        get => _melodicInversionRate;
        set => SetProperty(ref _melodicInversionRate, value);
    }

    public double SwapRate
    {
        get => _swapRate;
        set => SetProperty(ref _swapRate, value);
    }

    public double CrossoverRate
    {
        get => _crossoverRate;
        set => SetProperty(ref _crossoverRate, value);
    }

    public int MaxStepLength
    {
        get => _maxStepLength;
        set => SetProperty(ref _maxStepLength, value);
    }

    public int? Seed
    {
        get => _seed;
        set => SetProperty(ref _seed, value);
    }
}