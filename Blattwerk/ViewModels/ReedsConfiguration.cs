using System;
using System.ComponentModel.DataAnnotations;

namespace Blattwerk.ViewModels;

public enum ReedsType
{
    Piccolo,
    Flute,
    [Display(Name = "Alto flute")]
    AltoFlute,
    [Display(Name = "Bass flute")]
    BassFlute,
    Recorder,
    Oboe,
    [Display(Name = "English horn")]
    EnglishHorn,
    [Display(Name = "Clarinet Bb")]
    ClarinetBb,
    [Display(Name = "Clarinet A")]
    ClarenetA,
    [Display(Name = "Clarinet Eb")]
    ClarinetEb,
    [Display(Name = "Bass clarinet")]
    BassClarinet,
    Bassoon,
    [Display(Name = "Contra bassoon")]
    ContraBassoon,
    [Display(Name = "Soprano sax")]
    SopranoSax,
    [Display(Name = "Alto sax")]
    AltoSax,
    [Display(Name = "Tenor sax")]
    TenorSax,
    [Display(Name = "Baritone sax")]
    BaritoneSax,
    Shakuhachi,
    [Display(Name = "Pan flute")]
    PanFlute,
    Duduk

}

public sealed class ReedsConfiguration : PitchConfiguration
{
    private int _typeIndex = (int)ReedsType.TenorSax;
    public ReedsType Type => (ReedsType)_typeIndex;

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


    private static (int lower, int upper) GetRange(ReedsType type)
    {
        return type switch
        {
            ReedsType.Piccolo => (74, 108),
            ReedsType.Flute => (60, 98),
            ReedsType.AltoFlute => (55, 84),
            ReedsType.BassFlute => (48, 84),
            ReedsType.Recorder => (65, 91),
            ReedsType.Oboe => (58, 93),
            ReedsType.EnglishHorn => (52, 84),
            ReedsType.ClarinetBb => (50, 94),
            ReedsType.ClarenetA => (49, 93),
            ReedsType.ClarinetEb => (55, 99),
            ReedsType.BassClarinet => (36, 79),
            ReedsType.Bassoon => (34, 76),
            ReedsType.ContraBassoon => (22, 58),
            ReedsType.SopranoSax => (56, 88),
            ReedsType.AltoSax => (49, 81),
            ReedsType.TenorSax => (44, 76),
            ReedsType.BaritoneSax => (36, 69),
            ReedsType.Shakuhachi => (64, 86),
            ReedsType.PanFlute => (60, 84),
            ReedsType.Duduk => (57, 81),
            _ => throw new ArgumentOutOfRangeException($"{type}")
        };

    }
}