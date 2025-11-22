using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using EnvironmentalSequencer.Models;

namespace EnvironmentalSequencer.Converters;

public class TargetToBrushConverter : IValueConverter
{
    private static readonly Color Pitch = Colors.Red;
    private static readonly Color Velocity = Colors.Orange;
    private static readonly Color PitchBend = Colors.Blue ;
    private static readonly Color ControlChange = Colors.Green;
    private static readonly Color Default = Color.FromRgb(102, 102, 102);

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TargetValue target)
        {
            var color = target switch
            {
                TargetValue.Pitch => Pitch,
                TargetValue.Velocity => Velocity,
                TargetValue.PitchBend => PitchBend,
                TargetValue.ControlChange => ControlChange,
                _ => Default
            };
            return new SolidColorBrush(color);
        }

        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}