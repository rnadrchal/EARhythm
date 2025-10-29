using System.Numerics;

namespace ImageSequencer.Extensions;

public static class NoteNumberExtensions
{
    private static readonly string[] NoteNames = new string[]
    {
        "C ", "C+", "D ", "D+", "E ", "F ", "F+", "G ", "G+", "A ", "A+", "B "
    };

    public static string ToNoteNumberString(this int noteNumber)
    {
        if (noteNumber > 0)
        {
            var noteIndex = noteNumber % 12;
            return $"{NoteNames[noteIndex]}{noteNumber / 12 - 1: 00;-00; 00}";
        }
        return string.Empty;
    }
}