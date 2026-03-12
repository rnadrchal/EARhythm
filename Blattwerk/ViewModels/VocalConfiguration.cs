using System;
using System.ComponentModel.DataAnnotations;

namespace Blattwerk.ViewModels;

public enum VocalType
{
    Bass,
    Baritone,
    Tenor,
    Alto,
    [Display(Name = "Mezzo Soprano")]
    MezzoSoprano,
    Soprano
}
public class VocalConfiguration : PitchConfiguration
{
    public VocalType Type => (VocalType)_typeIndex;
    private int _typeIndex = (int)VocalType.MezzoSoprano;

    public int TypeIndex
    {
        get => _typeIndex;
        set
        {
            if (SetProperty(ref _typeIndex, value))
            {
                RaisePropertyChanged(nameof(Type));
            }
        }
    }
    public override int GetValue(float normalizedValue)
    {
        var (lower, upper) = GetRange(Type);
        return (int)Math.Clamp((Math.Round(lower + normalizedValue * Math.Abs(upper - lower))), lower, upper);
    }

    private static (int lower, int upper) GetRange(VocalType type)
    {
        return type switch
        {
            VocalType.Bass => (40, 64),
            VocalType.Baritone => (45, 65),
            VocalType.Tenor => (48, 69),
            VocalType.Alto => (53, 74),
            VocalType.MezzoSoprano => (57, 81),
            VocalType.Soprano => (60, 84),
            _ => throw new ArgumentOutOfRangeException($"{type}")
        };
    }
}