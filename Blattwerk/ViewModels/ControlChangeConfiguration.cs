using System;
using Prism.Mvvm;

namespace Blattwerk.ViewModels;

public sealed class ControlChangeConfiguration : BindableBase
{
    private int _number = 1;

    public int Number
    {
        get => _number;
        set => SetProperty(ref _number, value);
    }

    private int _minValue = 0;

    public int MinValue
    {
        get => _minValue;
        set
        {
            if (value < _maxValue)
                SetProperty(ref _minValue, value);
        }
    }

    private int _maxValue = 127;

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