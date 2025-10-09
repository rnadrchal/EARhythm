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
                RhythmGenerationMethod.Euclid => MaterialIconKind.LockPattern,
                RhythmGenerationMethod.Bernoulli => MaterialIconKind.DistributeHorizontalCenter,
                RhythmGenerationMethod.Poisson => MaterialIconKind.DistributeHorizontalLeft,
                RhythmGenerationMethod.CellullarAutomaton => MaterialIconKind.Grid,
                RhythmGenerationMethod.LSystem => MaterialIconKind.CodeBraces,
                RhythmGenerationMethod.Polyrhythm => MaterialIconKind.MusicNotePlus,
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