using System.Globalization;
using System.Windows.Data;
using Melanchall.DryWetMidi.Common;

namespace Egami.Sequencer.UI.Converters;

public class NoteNumberConverter : IValueConverter
{
    private static readonly string[] NoteNames = new string[]
    {
        "C", "C#", "D", "Eb", "E", "F", "F#", "G", "Ab", "A", "Bb", "B"
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is byte byteValue)
        {
            int noteIndex = byteValue % 12;
            return $"{NoteNames[noteIndex]}{byteValue / 12}";
        }

        if (value is int intValue)
        {
            int noteIndex = intValue % 12;
            return $"{NoteNames[noteIndex]}{intValue / 12}";
        }

        if (value is SevenBitNumber sbn)
        {
            int noteIndex = (byte)sbn % 12;
            return $"{NoteNames[noteIndex]}{(byte)sbn / 12}";
        }

        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}