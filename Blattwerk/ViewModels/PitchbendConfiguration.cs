using System;
using Prism.Mvvm;

namespace Blattwerk.ViewModels;

public sealed class PitchbendConfiguration : BindableBase
{
    private int _minValue;
    public int MinValue
    {
        get => _minValue;
        set
        {
            if (value < _maxValue)
                SetProperty(ref _minValue, value);
        }
    }

    private int _maxValue = 16383;
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