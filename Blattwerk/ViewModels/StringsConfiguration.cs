using System;
using System.ComponentModel.DataAnnotations;

namespace Blattwerk.ViewModels;

public enum StringsType
{
    Violin,
    Viola,
    Cello,
    [Display(Name = "Double bass")]
    DoubleBass,
    Guitar,
    ElectricBass,
    Ukulele,
    Mandolin,
    Banjo,
    Bouzuoki,
    Lute,
    Balalaika,
    [Display(Name = "Concert harp")]
    ConcertHarp,
    Autoharp,
    Zither,
    Sitar,
    Koto,
    Shamisen,
    Erhu,
    Pipa,
    Oud
}

public sealed class StringsConfiguration : PitchConfiguration
{
    public StringsType Type => (StringsType)_typeIndex;

    private int _typeIndex = (int)StringsType.Guitar;

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


    private static (int lower, int upper) GetRange(StringsType type)
    {
        return type switch
        {
            StringsType.Violin => (55, 100),
            StringsType.Viola => (48, 93),
            StringsType.Cello => (36, 81),
            StringsType.DoubleBass => (28, 72),
            StringsType.Guitar => (40, 88),
            StringsType.ElectricBass => (24, 67),
            StringsType.Ukulele => (67, 81),
            StringsType.Mandolin => (55, 95),
            StringsType.Banjo => (55, 81),
            StringsType.Bouzuoki => (43, 81),
            StringsType.Lute => (43, 74),
            StringsType.Balalaika => (64, 83),
            StringsType.ConcertHarp => (24, 103),
            StringsType.Autoharp => (41, 84),
            StringsType.Zither => (40, 93),
            _ => throw new ArgumentOutOfRangeException($"{type}")
        };
    }

}