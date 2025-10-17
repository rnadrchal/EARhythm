using System;
using System.Globalization;
using System.Windows.Data;
using Material.Icons;

namespace EuclidEA.Converters;

public class PitchGenerationToSymbolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value != null && int.TryParse(value.ToString(), out var index))
        {
            return index switch
            {
                0 => MaterialIconKind.NumericOneCircle,
                1 => MaterialIconKind.AlphaRCircle,
                2 => MaterialIconKind.ChartBellCurve, // Normal Distribution
                3 => MaterialIconKind.School,
                _ => MaterialIconKind.QuestionMark
            };
        }

        return MaterialIconKind.QuestionMark;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}