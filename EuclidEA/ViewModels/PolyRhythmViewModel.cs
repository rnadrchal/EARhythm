using System;
using Egami.Rhythm.Generation;

namespace EuclidEA.ViewModels;

public class PolyRhythmViewModel : RhythmGeneratorViewModel
{
    private int _a = 3;
    public int A
    {
        get => _a;
        set
        {
            if (value > Steps) value = Steps;
            SetProperty(ref _a, value);
        }
    }

    private int _b = 4;
    public int B
    {
        get => _b;
        set
        {
            if (value > Steps) value = Steps;
            SetProperty(ref _b, value);
        }
    }

    private byte _velA = 100;
    public byte VelA
    {
        get => _velA;
        set => SetProperty(ref _velA, value);
    }

    private byte _velB = 80;
    public byte VelB
    {
        get => _velB;
        set => SetProperty(ref _velB, value);
    }

    private int _lengthA = 1;

    public int LengthA
    {
        get => _lengthA;
        set => SetProperty(ref _lengthA, value);
    }

    private int _lengthB = 1;
    public int LengthB
    {
        get => _lengthB;
        set => SetProperty(ref _lengthB, value);
    }

    protected override IRhythmGenerator Generator => new PolyrhythmGenerator(Math.Min(_a, _steps), Math.Min(_b, _steps), _velA, _velB, _lengthA, _lengthB);
    public override string Name => "Polyrhythm Generator";
}