namespace Egami.EA.Metrics;

public interface IFitnessServiceOptions
{
    public int MaxCircularShift { get; }
    public int LMax { get; }
    public double W_Hits { get; }
    public double W_Pitch { get; }
    public double W_Vel { get; }
    public double W_Len { get; }
    public bool UseLethalThreshold { get; }
    public double HitsLethalThreshold { get;}
    public Weighting.CombineMode CombineMode { get; }
}