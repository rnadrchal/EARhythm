using Prism.Mvvm;

namespace StepMutator.Models.Evolution;

public class FitnessSettings : BindableBase
{
    private double _weightPitch = 100.0;
    public double WeightPitch
    {
        get => _weightPitch;
        set => SetProperty(ref _weightPitch, value);
    }

    private double _weightVelocity = 20.0;
    public double WeightVelocity
    {
        get => _weightVelocity;
        set => SetProperty(ref _weightVelocity, value);
    }

    private double _weightHit = 30.0;
    public double WeightHit
    {
        get => _weightHit;
        set => SetProperty(ref _weightHit, value);
    }

    private double _weightTie = 30.0;
    public double WeightTie
    {
        get => _weightTie;
        set => SetProperty(ref _weightTie, value);
    }

    private double _weightPitchbend = 0.0;
    public double WeightPitchbend
    {
        get => _weightPitchbend;
        set => SetProperty(ref _weightPitchbend, value);
    }

    private double _weightModulation = 0.0;
    public double WeightModulation
    {
        get => _weightModulation;
        set => SetProperty(ref _weightModulation, value);
    }
}