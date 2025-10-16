using System;
using System.Globalization;
using System.Windows.Data;
using EuclidEA.Services;
using Material.Icons;

namespace EuclidEA.Converters;

public class PatternGenerationToSymbolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {

        if (value != null &&int.TryParse(value.ToString(), out var i))
        {
            return (RhythmGenerationMethod)i switch
            {
                RhythmGenerationMethod.Euclid => MaterialIconKind.ArrowLeftRight,
                RhythmGenerationMethod.Bernoulli => MaterialIconKind.ChartBellCurve,
                RhythmGenerationMethod.Poisson => MaterialIconKind.ChartBellCurveCumulative,
                RhythmGenerationMethod.CellullarAutomaton => MaterialIconKind.DotsGrid,
                RhythmGenerationMethod.LSystem => MaterialIconKind.FormatTextVariant,
                RhythmGenerationMethod.Polyrhythm => MaterialIconKind.MusicNote,
                RhythmGenerationMethod.TrackChunk => MaterialIconKind.File,
                _ => MaterialIconKind.BlurLinear
            };
        }

        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}