using System;
using Prism.Mvvm;
using System.Linq;
using TextSequencer.Services;

namespace TextSequencer.ViewModels;

public class CharacterArray : BindableBase, ICharacterArray
{
    private readonly CharacterArray? _parent;
    public CharacterArray? Parent => _parent;

    private string _text = "";

    public string Text
    {
        get => _text;
        set
        {
            if (SetProperty(ref _text, value))
            {
                RaisePropertyChanged(nameof(Characters));
                RaisePropertyChanged(nameof(IndicesAlpha));
                RaisePropertyChanged(nameof(NoteNames));
                RaisePropertyChanged(nameof(IndicesPc));
                RaisePropertyChanged(nameof(MedianAlphaIndex));
                RaisePropertyChanged(nameof(MedianAlphaIndexChar));
                RaisePropertyChanged(nameof(MedianAlphaIndexNoteName));
                RaisePropertyChanged(nameof(MedianPcNoteName));
                RaisePropertyChanged(nameof(NoteCounts));
                RaisePropertyChanged(nameof(CharCounts));
                RaisePropertyChanged(nameof(SumAlpha));
                RaisePropertyChanged(nameof(Tempo));
            }
        }
    }

    public char[] Characters => _text.ToCharArray();

    public int[] IndicesAlpha => _text.Indices().ToArray();

    public int[] IndicesPc => _text.Indices().ChromaticIndices().ToArray();
    public string[] NoteNames => _text.Indices().ChromaticNoteNames().ToArray();
    public double MeanAlphaIndex => IndicesAlpha.Where(i => i >= 0).Select(i => (double)i).Average();
    public int MedianAlphaIndex => (int)IndicesAlpha.Median();
    public char MeanAlphaIndexChar => ((int)Math.Round(MeanAlphaIndex)).IndexToChar();

    public char MedianAlphaIndexChar => MedianAlphaIndex.IndexToChar();
    public string MedianAlphaIndexNoteName => (MedianAlphaIndex % 12).ToNoteName();
    public double MeanPc { get; }
    public int MedianPc => IndicesPc.Median();
    public string MeanPcNoteName => ((int)Math.Round(MeanAlphaIndex) % 12).ToNoteName();
    public string MedianPcNoteName => MedianPc.ToNoteName();

    public CharCount[] CharCounts =>
        _text.CharCounts().OrderByDescending(c => c.Count).ThenBy(c => c.Char).Distinct().ToArray();
    public NoteCount[] NoteCounts => _text.Indices().NoteCounts().Distinct().OrderByDescending(nc => nc.Count).ThenBy(nc => nc.Index).ToArray();

    public int SumAlpha => IndicesAlpha.Where(i => i >= 0).Sum();
    public int SumPc => IndicesPc.Where(i => i >= 0).Sum();

    public int Tempo
    {
        get
        {
            if (_parent == null) return 120;
            var parentIndices = _parent.IndicesAlpha.Where(i => i >= 0).ToArray();
            var parentAvg = parentIndices.Sum() / (double)parentIndices.Length;
            var myIndices = IndicesAlpha.Where(i => i >= 0).ToArray();
            var myAvg = myIndices.Sum() / (double)myIndices.Length;
            return (int)Math.Round(120.0 / parentAvg * myAvg);
        }
    }

    public CharacterArray(string text = "", CharacterArray? parent = null)
    {
        _text = text;
        _parent = parent;
    }

}