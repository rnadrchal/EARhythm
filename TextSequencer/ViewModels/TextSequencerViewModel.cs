using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Egami.Sequencer.Grid;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.MusicTheory;
using Syncfusion.Windows.Shared;
using TextSequencer.ViewModels.Players;

namespace TextSequencer.ViewModels;

public sealed class TextSequencerViewModel : CharacterArray
{
    private static Dictionary<string, IEnumerable<Interval>> _scales = new Dictionary<string, IEnumerable<Interval>>
    {
        { "Chromatic", ScaleIntervals.Chromatic },
        { "Major", ScaleIntervals.Major },
        { "Minor", ScaleIntervals.Minor }
    };

    private readonly BassPlayer _bassPlayer;
    private readonly PadPlayer _padPlayer;

    public BassPlayer BassPlayer => _bassPlayer;
    public PadPlayer PadPlayer => _padPlayer;

    public ICommand NextCommand { get; }
    public ICommand PrevCommand { get; }

    public TextSequencerViewModel()
    {
        FourBitNumber bassChannel = (FourBitNumber)0;
        FourBitNumber padChannel = (FourBitNumber)1;

        Text = "WORT-KLAU-BE-REI";

        SetSyllables();

        _bassPlayer = new BassPlayer(GridDivision.Sixteenth, bassChannel);
        _padPlayer = new PadPlayer(GridDivision.Sixteenth, padChannel);
        _bassPlayer.SetCharacterArray(CurrentSyllable);
        _padPlayer.SetCharacterArray(CurrentSyllable);


        PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(Text))
            {
                SetSyllables();
                _bassPlayer.SetCharacterArray(CurrentSyllable);
                _padPlayer.SetCharacterArray(CurrentSyllable);
            }
        };


        NextCommand = new DelegateCommand(_ =>
            SyllableIndex = _syllableIndex == Syllables.Count - 1 ? 0 : _syllableIndex + 1);
        PrevCommand = new DelegateCommand(_ =>
            SyllableIndex = _syllableIndex > 0 ? _syllableIndex - 1 : Syllables.Count - 1);
    }



    private int _syllableIndex;
    public int SyllableIndex
    {
        get => _syllableIndex;
        set
        {
            if (SetProperty(ref _syllableIndex, value))
            {
                RaisePropertyChanged(nameof(CurrentSyllable));
                _bassPlayer.SetCharacterArray(CurrentSyllable);
            }
        }
    }

    public CharacterArray CurrentSyllable => HasSyllables ? Syllables[SyllableIndex % Syllables.Count] : null;

    private void SetSyllables()
    {
        var syllables = Text.Split('-').Select(s => new CharacterArray(s, this));
        Syllables.Clear();
        foreach (var syllable in syllables)
        {
            Syllables.Add(syllable);
        }
        if (_syllableIndex >= Syllables.Count)
        {
            _syllableIndex =0;
        }
        RaisePropertyChanged(nameof(Tempo));
        RaisePropertyChanged(nameof(HasSyllables));
        RaisePropertyChanged(nameof(SyllableCount));
        RaisePropertyChanged(nameof(SyllableIndex));
        RaisePropertyChanged(nameof(CurrentSyllable));
    }

    public bool HasSyllables => Syllables.Count >0;

    public ObservableCollection<CharacterArray> Syllables { get; } = new();

    public int SyllableCount => Syllables.Count;

}