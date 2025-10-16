using Egami.EA.Metrics;
using Egami.EA.Metrics.Weighting;
using Prism.Mvvm;

namespace EuclidEA.Models;

public sealed class FitnessServiceOptions : BindableBase, IFitnessServiceOptions
{
    private int _maxCircularShift = 2;
    public int MaxCircularShift
    {
        get => _maxCircularShift;
        set => SetProperty(ref _maxCircularShift, value);
    }

    private int _lMax = 32;
    public int LMax
    {
        get => _lMax;
        set => SetProperty(ref _lMax, value);
    }

    private double _wHits = 0.40;
    public double W_Hits
    {
        get => _wHits;
        set => SetProperty(ref _wHits, value);
    }

    private double _wPitch = 0.25;
    public double W_Pitch
    {
        get => _wPitch;
        set => SetProperty(ref _wPitch, value);
    }

    private double _wVel = 0.20;
    public double W_Vel
    {
        get => _wVel;
        set => SetProperty(ref _wVel, value);
    }

    private double _wLen = 0.15;
    public double W_Len
    {
        get => _wLen;
        set => SetProperty(ref _wLen, value);
    }

    private bool _useLethalThreshold = true;
    public bool UseLethalThreshold
    {
        get => _useLethalThreshold;
        set => SetProperty(ref _useLethalThreshold, value);
    }

    private double _hitsLethalThreshold = 0.15;
    public double HitsLethalThreshold
    {
        get => _hitsLethalThreshold;
        set => SetProperty(ref _hitsLethalThreshold, value);
    }

    private CombineMode _combineMode = CombineMode.Arithmetic;
    public CombineMode CombineMode
    {
        get => _combineMode;
        set => SetProperty(ref _combineMode, value);
    }

    private bool _isGeometric;
    public bool IsGeometric
    {
        get => _isGeometric;
        set
        {
            if (SetProperty(ref _isGeometric, value) && value)
            {
                CombineMode = value ? CombineMode.Geometric : CombineMode.Arithmetic;
            }
        }
    }
}