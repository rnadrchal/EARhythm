using System;
using System.Globalization;
using System.Windows.Data;

namespace EuclidEA.Converters;

public class NoteNameConverter : IValueConverter
{
    private static string[] NoteNames = new string[]
    {
        "C", "C#", "D", "Eb", "E", "F", "F#", "G", "Ab", "A", "Bb", "B"
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int intValue)
        {
            int noteIndex = intValue % 12;
            return NoteNames[noteIndex];
        }

        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}