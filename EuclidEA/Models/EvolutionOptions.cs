using Egami.Rhythm.EA;
using Prism.Mvvm;

namespace EuclidEA.Models;

public class EvolutionOptions : BindableBase, IEvolutionOptions
{
    private double _mutationRate = 0.1;
    private double _deletionRate = 0.01;
    private double _insertionRate = 0.01;
    private double _lengthRate = 0.02;
    private double _swapRate = 0.05;
    private double _crossoverRate = 0.7;
    private int? _seed;

    public double MutationRate
    {
        get => _mutationRate;
        set => SetProperty(ref _mutationRate, value);
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

    public double LengthRate
    {
        get => _lengthRate;
        set => SetProperty(ref _lengthRate, value);
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

    public int? Seed
    {
        get => _seed;
        set => SetProperty(ref _seed, value);
    }
}