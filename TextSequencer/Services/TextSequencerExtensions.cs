using System;
using System.Collections.Generic;
using System.Linq;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.MusicTheory;

namespace TextSequencer.Services;

public record NoteCount(int Index, string NoteName, int Count);

public record CharCount(int Index, char Char, int Count);

public static class StringExtensions
{
    public static IEnumerable<int> Indices(this string s)
    {
        return s.Select(c =>
        {
            if (c is >= 'A' and <= 'z') return c - 'A';
            if (c == 'Ä') return 'z' + 1;
            if (c == 'Ö') return 'z' + 2;
            if (c == 'Ü') return 'z' + 3;
            if (c == 'ä') return 'z' + 4;
            if (c == 'ö') return 'z' + 5;
            if (c == 'ü') return 'z' + 6;
            if (c == 'ß') return 'z' + 7;
            return -1;
        });
    }

    public static char IndexToChar(this int index)
    {
        if (index <= 'z' - 'A') return (char)(index + 'A');
        return index switch
        {
            'z' + 1 => 'Ä',
            'z' + 2 => 'Ö',
            'z' + 3 => 'Ü',
            'z' + 4 => 'ä',
            'z' + 5 => 'ö',
            'z' + 6 => 'ü',
            'z' + 7 => 'ß',
            _ => (char)0
        };
    }

    public static IEnumerable<CharCount> CharCounts(this string s)
    {
        return Indices(s).Where(i => i >= 0)
            .Select(i =>
            {
                var c = i.IndexToChar();
                return new CharCount(
                    i,
                    c,
                    s.Count(x => x == c));
            });
    }

    public static int Median(this IEnumerable<int> source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        var arr = source.Where(i => i != -1).ToArray();
        if (arr.Length == 0) return 0;
        Array.Sort(arr);
        int mid = arr.Length / 2;
        if (arr.Length % 2 == 1)
            return arr[mid];
        // even count: return rounded average of two middle values
        return (int)Math.Round((arr[mid - 1] + arr[mid]) / 2.0);
    }


    public static IEnumerable<int> ChromaticIndices(this IEnumerable<int> indices)
    {
        return indices.Select(i => i % 12);
    }

    public static string ToNoteName(this int i) => i >= 0 ? ((NoteName)(i % 12)).ToString().Replace("Sharp", "#") : "";


    public static IEnumerable<string> ChromaticNoteNames(this IEnumerable<int> indices)
    {
        return indices.ChromaticIndices().Select(i => i.ToNoteName());
    }

    public static IEnumerable<NoteCount> NoteCounts(this IEnumerable<int> indices)
    {
        var chromaticIndices = indices.ChromaticIndices().ToArray();
        foreach (var i in chromaticIndices.Where(i => i >= 0))
        {
            yield return new NoteCount(i, i.ToNoteName(), chromaticIndices.Count(x => x == i));
        }
    }


    private static Dictionary<char, byte> _charToVelocityMap = new()
    {
        // Vowels (open vowels louder)
        ['a'] = 120,
        ['e'] = 110,
        ['i'] = 100,
        ['o'] = 122,
        ['u'] = 105,
        ['ä'] = 118,
        ['ö'] = 121,
        ['ü'] = 107,
        ['y'] = 98, // sometimes vowel

        // Uppercase umlauts (explicit entries to be safe)
        ['Ä'] = 118,
        ['Ö'] = 121,
        ['Ü'] = 107,
        ['ẞ'] = 78, // uppercase sharp S

        // Plosives (strong attack)
        ['b'] = 105,
        ['p'] = 115,
        ['d'] = 103,
        ['t'] = 112,
        ['g'] = 104,
        ['k'] = 111,
        ['c'] = 95,
        ['q'] = 95,

        // Fricatives (softer, sibilant)
        ['f'] = 70,
        ['s'] = 75,
        ['ß'] = 78,
        ['v'] = 76,
        ['z'] = 85,
        ['x'] = 92,
        ['h'] = 40,

        // Nasals
        ['m'] = 88,
        ['n'] = 86,

        // Liquids and approximants
        ['l'] = 82,
        ['r'] = 80,
        ['j'] = 72,
        ['w'] = 76
    };

    public static SevenBitNumber CharToVelocity(this char c)
    {
        if (_charToVelocityMap.TryGetValue(c, out var v))
            return (SevenBitNumber)v;

        // try lowercase fallback
        var lc = char.ToLowerInvariant(c);
        if (_charToVelocityMap.TryGetValue(lc, out v))
            return (SevenBitNumber)v;

        // unknown character
        return (SevenBitNumber)0    ;
    }
}