using TextSequencer.Services;

namespace TextSequencer.ViewModels;

public interface ICharacterArray
{
    string Text { get; }

    char[] Characters { get; }

    int[] IndicesAlpha { get; }

    int[] IndicesPc { get; }

    string[] NoteNames { get; }

    int MedianAlphaIndex { get; }

    char MedianAlphaIndexChar { get; }

    string MedianAlphaIndexNoteName { get; }
    int MedianPc { get; }

    string MedianPcNoteName { get; }

    CharCount[] CharCounts { get; }
    NoteCount[] NoteCounts { get; }
    int SumAlpha { get; }
    int SumPc { get; }
}