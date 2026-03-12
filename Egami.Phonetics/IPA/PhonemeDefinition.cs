namespace Egami.Phonetics.IPA;

internal enum PhonemeKind
{
    Vowel,
    Diphthong,
    Consonant
}

internal enum ConsonantClass
{
    Plosive,
    Fricative,
    Nasal,
    Liquid,    // l / r
    Other
}

internal sealed record PhonemeDefinition(
    double RelativeDuration,
    double RelativeLoudness,
    int? StartDegreeOffset,   // für Glides: Start
    int? EndDegreeOffset,     // für Glides: Ziel; bei Monophthong == Start
    PhonemeKind Kind,
    ConsonantClass? ConsonantClass);