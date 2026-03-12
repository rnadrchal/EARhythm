using System;
using System.CodeDom;
using System.Windows.Automation;
using Melanchall.DryWetMidi.Standards;

namespace Blattwerk.ViewModels;

public enum SetType
{
    Standard,
    Basic,
    Minimal,
    MinimalTechno,
    Full
}

public class DrumkitConfiguration : PitchConfiguration
{
    private int[] _pitchesFullSet =
    [
        35,
        36,
        37,
        38,
        39,
        40,
        41,
        42,
        43,
        44,
        45,
        46,
        47,
        48,
        49,
        50,
        51,
        52,
        53,
        55,
        56,
        57,
        59
    ];

    private int[] _pitchesStandardSet =
    [
        36,
        37,
        38,
        41,
        42,
        44,
        46,
        47,
        49,
        51
    ];

    private int[] _pitchesBasicSet =
    [
        36,
        38,
        42,
        44,
        46
    ];

    private int[] _pitchesMinimalSet =
    [
        36,
        38,
        42
    ];

    private int[] _pitchesMinimalTechnoSet =
    [
        36,
        39,
        40,
        42,
        46
    ];

    public SetType Type => (SetType)_typeIndex;

    private int _typeIndex = (int)SetType.Standard;

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
        var pitches = Type switch
        {
            SetType.Standard => _pitchesStandardSet,
            SetType.Basic => _pitchesBasicSet,
            SetType.Minimal => _pitchesMinimalSet,
            SetType.MinimalTechno => _pitchesMinimalTechnoSet,
            SetType.Full => _pitchesFullSet,
            _ => throw new InvalidOperationException(Type.ToString())
        };
        var index = Math.Clamp((int)(pitches.Length * normalizedValue), 0, pitches.Length - 1);
        return pitches[index];
    }
}