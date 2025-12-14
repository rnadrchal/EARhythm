using TextSequencer.Services;

namespace TextSequencer.ViewModels;

public interface ICharacterArray
{
    string Text { get; }

    char[] Characters { get; }

    int[] IndicesAlpha { get; }

    int[] IndicesPc { get; }

    string[] NoteNames { get; }

    double MeanAlphaIndex { get; }
    int MedianAlphaIndex { get; }

    char MeanAlphaIndexChar { get; }
    char MedianAlphaIndexChar { get; }

    string MedianAlphaIndexNoteName { get; }
    double MeanPc { get; }
    int MedianPc { get; }

    string MeanPcNoteName { get; }
    string MedianPcNoteName { get; }

    CharCount[] CharCounts { get; }
    NoteCount[] NoteCounts { get; }
    int SumAlpha { get; }
    int SumPc { get; }
}