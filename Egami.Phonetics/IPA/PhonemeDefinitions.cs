namespace Egami.Phonetics.IPA;

internal static class PhonemeDefinitions
{
    public static readonly IReadOnlyDictionary<string, PhonemeDefinition> Map =
        CreateMap();

    private static IReadOnlyDictionary<string, PhonemeDefinition> CreateMap()
    {
        var dict = new Dictionary<string, PhonemeDefinition>
        {
            // --- VOKALE (Start = End) ---

            ["iː"] = new(1.7, 1.10, 6, 6, PhonemeKind.Vowel, null),
            ["ɪ"] = new(0.9, 1.00, 3, 3, PhonemeKind.Vowel, null),

            ["eː"] = new(1.6, 1.05, 5, 5, PhonemeKind.Vowel, null),
            ["ɛ"] = new(1.1, 1.00, 3, 3, PhonemeKind.Vowel, null),

            ["aː"] = new(1.8, 1.15, 2, 2, PhonemeKind.Vowel, null),
            ["a"] = new(1.2, 1.05, 2, 2, PhonemeKind.Vowel, null),

            ["oː"] = new(1.6, 1.05, 1, 1, PhonemeKind.Vowel, null),
            ["ɔ"] = new(1.1, 1.00, 0, 0, PhonemeKind.Vowel, null),

            ["uː"] = new(1.6, 1.05, 0, 0, PhonemeKind.Vowel, null),
            ["ʊ"] = new(0.9, 0.95, -1, -1, PhonemeKind.Vowel, null),

            ["yː"] = new(1.6, 1.05, 5, 5, PhonemeKind.Vowel, null),
            ["ʏ"] = new(1.0, 0.95, 2, 2, PhonemeKind.Vowel, null),

            ["øː"] = new(1.6, 1.05, 4, 4, PhonemeKind.Vowel, null),
            ["œ"] = new(1.0, 0.95, 2, 2, PhonemeKind.Vowel, null),

            ["ə"] = new(0.6, 0.80, 0, 0, PhonemeKind.Vowel, null),
            ["ɐ"] = new(0.8, 0.90, -1, -1, PhonemeKind.Vowel, null),

            // --- DIPHTHONGE (Start != End) ---

            ["aɪ̯"] = new(1.4, 1.05, 2, 6, PhonemeKind.Diphthong, null),
            ["aɪ"] = new(1.4, 1.05, 2, 6, PhonemeKind.Diphthong, null),

            ["aʊ̯"] = new(1.4, 1.05, 2, 0, PhonemeKind.Diphthong, null),
            ["aʊ"] = new(1.4, 1.05, 2, 0, PhonemeKind.Diphthong, null),

            ["ɔʏ̯"] = new(1.3, 1.00, 0, 5, PhonemeKind.Diphthong, null),
            ["ɔʏ"] = new(1.3, 1.00, 0, 5, PhonemeKind.Diphthong, null),

            // --- KONSONANTEN ---

            // Plosive
            ["p"] = new(0.25, 1.10, null, null, PhonemeKind.Consonant, ConsonantClass.Plosive),
            ["t"] = new(0.25, 1.10, null, null, PhonemeKind.Consonant, ConsonantClass.Plosive),
            ["k"] = new(0.25, 1.10, null, null, PhonemeKind.Consonant, ConsonantClass.Plosive),
            ["b"] = new(0.30, 1.05, null, null, PhonemeKind.Consonant, ConsonantClass.Plosive),
            ["d"] = new(0.30, 1.05, null, null, PhonemeKind.Consonant, ConsonantClass.Plosive),
            ["g"] = new(0.30, 1.05, null, null, PhonemeKind.Consonant, ConsonantClass.Plosive),

            // Frikative
            ["f"] = new(0.50, 1.00, null, null, PhonemeKind.Consonant, ConsonantClass.Fricative),
            ["s"] = new(0.50, 1.00, null, null, PhonemeKind.Consonant, ConsonantClass.Fricative),
            ["z"] = new(0.55, 1.05, null, null, PhonemeKind.Consonant, ConsonantClass.Fricative),
            ["ʃ"] = new(0.60, 1.00, null, null, PhonemeKind.Consonant, ConsonantClass.Fricative),
            ["ç"] = new(0.50, 0.95, null, null, PhonemeKind.Consonant, ConsonantClass.Fricative),
            ["x"] = new(0.55, 1.00, null, null, PhonemeKind.Consonant, ConsonantClass.Fricative),
            ["h"] = new(0.40, 0.90, null, null, PhonemeKind.Consonant, ConsonantClass.Fricative),

            // Nasale
            ["m"] = new(0.75, 1.00, null, null, PhonemeKind.Consonant, ConsonantClass.Nasal),
            ["n"] = new(0.75, 1.00, null, null, PhonemeKind.Consonant, ConsonantClass.Nasal),
            ["ŋ"] = new(0.80, 1.00, null, null, PhonemeKind.Consonant, ConsonantClass.Nasal),

            // Liquide (r/l)
            ["l"] = new(0.85, 1.05, null, null, PhonemeKind.Consonant, ConsonantClass.Liquid),
            ["ʁ"] = new(0.80, 1.05, null, null, PhonemeKind.Consonant, ConsonantClass.Liquid),
            ["r"] = new(0.80, 1.05, null, null, PhonemeKind.Consonant, ConsonantClass.Liquid),

            // Sonstiges
            ["ts"] = new(0.70, 1.10, null, null, PhonemeKind.Consonant, ConsonantClass.Other),
            ["tʃ"] = new(0.75, 1.10, null, null, PhonemeKind.Consonant, ConsonantClass.Other),
            ["pf"] = new(0.70, 1.05, null, null, PhonemeKind.Consonant, ConsonantClass.Other),
            ["ʔ"] = new(0.20, 1.00, null, null, PhonemeKind.Consonant, ConsonantClass.Other)
        };

        return dict;
    }
}