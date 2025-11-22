using Prism.Mvvm;

namespace EnvironmentalSequencer.Models;

public enum TargetValue
{
    None,
    Pitch,
    Velocity,
    ControlChange,
    PitchBend,
}

public abstract class ValueMapping
    : BindableBase
{
    public string Name { get; private set; }
    public double MinValue { get; private set; }
    public double MaxValue { get; private set; }
    public string Unit { get; private set; }

    protected double _value = 0;

    public virtual double Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }

    private int _targetValue = 0;

    public int TargetValue
    {
        get => _targetValue;
        set
        {
            if (SetProperty(ref _targetValue, value))
            {
                RaisePropertyChanged(nameof(Target));
            }
        }

    }

    public TargetValue Target => (TargetValue)TargetValue;

    public ValueMapping(string name, double value, string unit, double maxValue, double minValue = 0)
    {
        _value = value;
        Name = name;
        Unit = unit;
        MaxValue = maxValue;
        MinValue = minValue;
    }

    public abstract byte ToByte();
    public abstract ushort ToUshort();
}