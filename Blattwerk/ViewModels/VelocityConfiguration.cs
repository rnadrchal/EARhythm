using System;
using Prism.Mvvm;

namespace Blattwerk.ViewModels;

public sealed class VelocityConfiguration : BindableBase
{
    private int _value = 60;

    private int _minValue = 20;
    public int MinValue
    {
        get => _minValue;
        set 
        {
            if (value < _maxValue)
                SetProperty( ref _minValue, value);
        }
    }

    private int _maxValue = 100;
    public int MaxValue
    {
        get => _maxValue;
        set
        {
            if (value > _minValue) 
                SetProperty(ref _maxValue, value);
        }
    }

    public int GetValue(float normalizedValue) => (int)(_minValue + normalizedValue * Math.Abs(_maxValue - _minValue));
}