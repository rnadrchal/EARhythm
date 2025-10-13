using Egami.Pitch;

namespace EuclidEA.ViewModels;

public class NormalDistributionPitchGeneratorViewModel : PitchGeneratorViewModel
{
    private readonly NormalDistributionPitchGenerator _ndg;
    private int _stdDev = 7;
    public int StandardDeviation
    {
        get => _stdDev;
        set
        {
            if (SetProperty(ref _stdDev, value))
            {
                _ndg.StandardDeviation = value;
            }
        }
    }

    private double _skewness = 0.0;
    public double Skewness
    {
        get => _skewness;
        set
        {
            if (SetProperty(ref _skewness, value))
            {
                _ndg.Skewness = value;
            }
        }
    }

    private double _kurtosis = 0.0;
    public double Kurtosis
    {
        get => _kurtosis;
        set
        {
            if (SetProperty(ref _kurtosis, value))
            {
                _ndg.Kurtosis = value;
            }
        }
    }

    public NormalDistributionPitchGeneratorViewModel(IPitchGenerator generator) : base(generator)
    {
        _ndg = generator as NormalDistributionPitchGenerator ?? new NormalDistributionPitchGenerator();
    }

    public override string Name => "Normal Distribution";
}