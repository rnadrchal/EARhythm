using System;
using Egami.Rhythm.Generation;

namespace EuclidEA.ViewModels;

public class CellularAutomatonViewModel : RhythmGeneratorViewModel
{
    private int _generations;
    public int Generations
    {
        get => _generations;
        set => SetProperty(ref _generations, value);
    }
    public CaRule[] Rules => Enum.GetValues<CaRule>();
    private int _rule;

    public int RuleIndex
    {
        get => _rule;
        set
        {
            if (SetProperty(ref _rule, value))
            {
                RaisePropertyChanged(nameof(Rule));
            }

        }
    }

    public int Rule => (int)Rules[_rule];

    public CaBoundary[] Boundaries => Enum.GetValues<CaBoundary>();
    public CaSeed[] Seeds => Enum.GetValues<CaSeed>();
    public CaMapMode[] MapModes => Enum.GetValues<CaMapMode>();

    private int _boundary;
    public int BoundaryIndex
    {
        get => _boundary;
        set
        {
            if (SetProperty(ref _boundary, value))
            {
                RaisePropertyChanged(nameof(Boundary));
            }
        }
    }
    public CaBoundary Boundary => Boundaries[_boundary];

    private int _seed;
    public int SeedIndex
    {
        get => _seed;
        set
        {
            if (SetProperty(ref _seed, value))
            {
                RaisePropertyChanged(nameof(Seed));
            }
        }
    }
    public CaSeed Seed => Seeds[_seed];

    private int _mapMode;
    public int MapModeIndex
    {
        get => _mapMode;
        set
        {
            if (SetProperty(ref _mapMode, value))
            {
                RaisePropertyChanged(nameof(MapMode));
            }
        }
    }
    public CaMapMode MapMode => MapModes[_mapMode];

    private int _n = 2;
    public int N
    {
        get => _n;
        set
        {
            if (value > Steps) value = Steps;
            SetProperty(ref _n, value); 

        }
    }

    protected override IRhythmGenerator Generator => new CellularAutomatonGenerator(_generations, (CaRule)Rule, (CaBoundary)Boundary, (CaSeed)Seed, (CaMapMode)MapMode);
    public override string Name => "Cellular Automaton";
}