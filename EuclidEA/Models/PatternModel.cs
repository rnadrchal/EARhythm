using System;
using Melanchall.DryWetMidi.Standards;
using Prism.Mvvm;

namespace EuclidEA.Models;

public class PatternModel : BindableBase
{
    public PatternModel(int divider = 8, int steps = 16)
    {
        _divider = divider;
        _steps = steps;
    }

    private int _divider;

    public int Divider
    {
        get => _divider;
        set => SetProperty(ref _divider, value);
    }

    private int _steps;
    public int Steps
    {
        get => _steps;
        set
        {
            if (SetProperty(ref _steps, value))
            {
                //Generate();
            }
        }
    }

    private int _rotation;
    public int Rotation
    {
        get => _rotation;
        set
        {
            if (SetProperty(ref _rotation, value))
            {
                //Generate();
            }
        }
    }

    private bool[] _hits;

    public bool[] Hits
    {
        get => _hits;
        private set => SetProperty(ref _hits, value);
    }

}