using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Egami.Rhythm.Midi;
using Egami.Sequencer;
using Egami.Sequencer.Grid;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.MusicTheory;
using Syncfusion.Windows.Shared;
using TextSequencer.Services;
using TextSequencer.Services.SequenceGeneration;

namespace TextSequencer.ViewModels;

public sealed class TextSequencerViewModel : CharacterArray
{
    private static Dictionary<string, IEnumerable<Interval>> _scales = new Dictionary<string, IEnumerable<Interval>>
    {
        { "Chromatic", ScaleIntervals.Chromatic },
        { "Major", ScaleIntervals.Major },
        { "Minor", ScaleIntervals.Minor }
    };

    private readonly IMusicalSequenceFromChars _bassSequenceGenerator = new OstinatoBassFromChars();
    private readonly MidiClockGrid _bassSequenceClock;
    private readonly GridSequencePlayer _bassPlayer;
    private readonly MidiClockGrid _bassLfoClock;
    private readonly GridLfoPlayer _bassLfoPlayer;

    public ICommand NextCommand { get; }
    public ICommand PrevCommand { get; }

    public TextSequencerViewModel()
    {
        FourBitNumber baseChannel = (FourBitNumber)0;
        _bassSequenceClock = new MidiClockGrid(_gridDivision);
        _bassLfoClock = new MidiClockGrid(_baseDivision);
        Text = "WORT-KLAU-BE-REI";

        SetSyllables();
        _bassPlayer = new GridSequencePlayer(
            _bassSequence,
            MidiDevices.Output,
            baseChannel,
            _bassSequenceClock);
        _bassLfoPlayer = new GridLfoPlayer(
            MidiDevices.Output,
            baseChannel,
            _bassLfoClock);

        GenerateSequences();


        PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(Text))
            {
                SetSyllables();
                GenerateSequences();
            }
        };

        MidiDevices.Input.EventReceived += OnMidiClock;

        NextCommand = new DelegateCommand(_ =>
            SyllableIndex = _syllableIndex == Syllables.Count - 1 ? 0 : _syllableIndex + 1);
        PrevCommand = new DelegateCommand(_ =>
            SyllableIndex = _syllableIndex > 0 ? _syllableIndex - 1 : Syllables.Count - 1);
    }

    private void OnMidiClock(object sender, MidiEventReceivedEventArgs e)
    {
        if (e.Event is TimingClockEvent)
        {
            _bassLfoClock.OnClockPulse();
            _bassSequenceClock.OnClockPulse();
        }

        if (e.Event is StartEvent)
        {
            _bassPlayer.Start();
            _bassLfoPlayer.Start();
        }
        if (e.Event is StopEvent)
        {
            _bassPlayer.Stop();
            _bassLfoPlayer.Stop();
        }
    }

    private void GenerateSequences()
    {
        _bassSequence = _bassSequenceGenerator.Generate(CurrentSyllable);
        _bassPlayer.SetSequence(_bassSequence, resetPosition: true);

        // Map character indices deviations to signed pitchbend displacement.
        // Assumptions:
        // - deviation (i - mean) is in semitones
        // -1 semitone =100 cents
        // - full pitchbend range (signed) corresponds to PB_RANGE_CENTS (e.g.400 cents)
        // - signed displacement range for pitchbend is -8192..+8191
        const double PB_RANGE_CENTS =400.0; // full span in cents (user-defined)
        const int PB_MAX =8191; // signed maximum displacement

        var baseAmplitudes = CurrentSyllable.IndicesAlpha
            .Select(i => (i - CurrentSyllable.MedianAlphaIndex)) // deviation in semitones
            .Select(semitoneDev => semitoneDev *100.0) // to cents
            .Select(cents => (int)Math.Round((cents / PB_RANGE_CENTS) * PB_MAX))
            .Select(a => Math.Clamp(a, -8192,8191))
            .ToArray();

        // Determine number of whole-note steps that cover the bass sequence.
        int sequenceSteps = Math.Max(1, _bassSequence.LengthInSteps);
        int stepsPerWholeForSequence = StepsPerWhole(_bassSequenceClock.Division);
        int wholeNotes = (int)Math.Ceiling(sequenceSteps / (double)stepsPerWholeForSequence);

        // Expand amplitudes so there is one amplitude value per whole-note step of the bass sequence.
        int[] amplitudesExpanded;
        if (baseAmplitudes.Length ==0)
        {
            amplitudesExpanded = Enumerable.Repeat(0, wholeNotes).ToArray();
        }
        else
        {
            amplitudesExpanded = Enumerable.Range(0, wholeNotes)
                .Select(idx => baseAmplitudes[idx % baseAmplitudes.Length])
                .ToArray();
        }

        var lfoBass = new LfoDefinition
        {
            Waveform = LfoWaveform.Sine,
            Amplitudes = amplitudesExpanded,
            TargetType = LfoTargetType.PitchBend,
            StepIndex =0,
            // LengthInSteps is in LFO grid units (LFO clock is set to Whole), so set to number of whole notes
            LengthInSteps = wholeNotes
        };
        _bassLfoPlayer.SetDefinitions(new[] { lfoBass });
    }

    private static int StepsPerWhole(GridDivision division)
    {
        // Map GridDivision to number of steps per whole note
        return division switch
        {
            GridDivision.ThirtySecond =>32,
            GridDivision.SixteenthTriplet =>24, //16th triplet ->3 per8th? approximate as24
            GridDivision.Sixteenth =>16,
            GridDivision.Eighth =>8,
            _ =>16
        };
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
                GenerateSequences();
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

    private MusicalSequence _bassSequence = MusicalSequence.Empty;

    #region MIDI Sequence Properties

    private GridDivision _gridDivision = GridDivision.Sixteenth;
    public GridDivision GridDivision
    {
        get => _gridDivision;
        set => SetProperty(ref _gridDivision, value);
    }

    private GridDivision _baseDivision = GridDivision.Whole;
    public GridDivision BaseDivision
    {
        get => _baseDivision;
        set => SetProperty(ref _baseDivision, value);
    }

    #endregion
}