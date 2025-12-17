using System;
using System.Linq;
using Egami.Rhythm.Midi;
using Egami.Sequencer;
using Egami.Sequencer.Grid;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using TextSequencer.Services.SequenceGeneration;

namespace TextSequencer.ViewModels.Players;

public sealed class BassPlayer : PlayerBase
{
    private readonly IMusicalSequenceFromChars _bassSequenceGenerator = new OstinatoBassFromChars();
    private readonly MidiClockGrid _lfoClock;
    private readonly GridSequencePlayer _bassPlayer;
    private readonly GridLfoPlayer _lfoPlayer;


    private MusicalSequence _bassSequence = MusicalSequence.Empty;

    public override string Title => "Bass";

    public string Note => CharacterArray.MedianPcNoteName;
    public int Bars => CharacterArray.SumAlpha;

    public GridDivision[] AvailableDivisions => Enum.GetValues<GridDivision>();

    private GridDivision _lfoDivision = GridDivision.Quarter;
    public GridDivision LfoDivision
    {
        get => _lfoDivision;
        set
        {
            if (SetProperty(ref _lfoDivision, value))
            {
                _lfoClock.SetDivision(value);
            }
        }
    }

    public LfoWaveform[] AvailableWaveforms => Enum.GetValues<LfoWaveform>();

    private LfoWaveform _lfoWaveform = LfoWaveform.Square;

    public LfoWaveform LfoWaveform
    {
        get => _lfoWaveform;
        set
        {
            if (SetProperty(ref _lfoWaveform, value))
            {
                UpdateLfo(CharacterArray);
            }
        }
    }

    private double _pulseWidth = 0.5;

    public double PulseWidth
    {
        get => _pulseWidth;
        set
        {
            if (SetProperty(ref _pulseWidth, value))
            {
                UpdateLfo(CharacterArray);
            }
        }
    }

    private double _maxCents = 50.0;

    public double MaxCents
    {
        get => _maxCents;
        set
        {
            if (SetProperty(ref _maxCents, value))
            {
                UpdateLfo(CharacterArray);
            }
        }
    }

    private double _phase = 0.0;
    public double Phase
    {
        get => _phase;
        set
        {
            if (SetProperty(ref _phase, value))
            {
                UpdateLfo(CharacterArray);
            }
        }
    }


    public BassPlayer(GridDivision division, FourBitNumber channel) : base(division, channel)
    {
        _lfoClock = new MidiClockGrid(_lfoDivision);
        _bassPlayer = new GridSequencePlayer(
            _bassSequence,
            MidiDevices.Output,
            channel,
            NoteClock); 
        _lfoPlayer = new GridLfoPlayer(
            MidiDevices.Output,
            channel,
            _lfoClock);
    }

    public override void SetCharacterArray(ICharacterArray characterArray)
    {
        base.SetCharacterArray(characterArray);

        UpdateSequence(characterArray);
        UpdateLfo(characterArray);
    }

    private void UpdateSequence(ICharacterArray characterArray)
    {
        _bassSequence = _bassSequenceGenerator.Generate(characterArray);
        _bassPlayer.SetSequence(_bassSequence, resetPosition: true);

        RaisePropertyChanged(nameof(Note));
        RaisePropertyChanged(nameof(Bars));
        RaisePropertyChanged(nameof(CharIndexDeviations));
    }

    public string CharIndexDeviations => string.Join(", ", CharacterArray.IndicesAlpha
        .Select(i => i - CharacterArray.MedianAlphaIndex));

    private void UpdateLfo(ICharacterArray characterArray)
    {
        // Map character indices deviations to signed pitchbend displacement.
        // Assumptions:
        // - deviation (i - mean) is in semitones
        // -1 semitone =100 cents
        // - full pitchbend range (signed) corresponds to PB_RANGE_CENTS (e.g.400 cents)
        // - signed displacement range for pitchbend is -8192..+8191

        int cent100 = (int)(_maxCents * 8192 / 200.0);
        var cents = characterArray.IndicesAlpha
            .Select(i => ((i - characterArray.MedianAlphaIndex) * cent100 / 12))
            .ToArray();

        // Use the short amplitude sequence (e.g.4 amplitudes) and loop it independently
        // The LFO will perform one waveform cycle over PeriodInWholeNotes whole notes and
        // will iterate the amplitude array cyclically per whole-note.
        var lfoBass = new LfoDefinition
        {
            Waveform = _lfoWaveform,
            Amplitudes = cents.Length >0 ? cents : new[] {0 },
            TargetType = LfoTargetType.PitchBend,
            StepIndex =0,
            // keep LFO running independently of the bass sequence length
            LengthInSteps = int.MaxValue,
            // set period to4 whole notes (one cycle =4 whole notes)
            PeriodInWholeNotes = PeriodsPerWholeNote(_lfoDivision),
            Phase = _phase,
            PulseWidth = _pulseWidth
        };
        _lfoPlayer.SetDefinitions(new[] { lfoBass });
    }

    protected override void OnMidiClock(object sender, MidiEventReceivedEventArgs e)
    {
        if (e.Event is TimingClockEvent)
        {
            _lfoClock.OnClockPulse();
        }

        if (e.Event is StartEvent)
        {
            _bassPlayer.Start();
            _lfoPlayer.Start();
        }
        if (e.Event is StopEvent)
        {
            _bassPlayer.Stop();
            _lfoPlayer.Stop();
        }

        base.OnMidiClock(sender, e);
    }

    public static double PeriodsPerWholeNote(GridDivision division)
    {
        return division switch
        {

            GridDivision.Whole => 1.0,
            GridDivision.Half => 2.0,
            GridDivision.Quarter => 4.0,
            GridDivision.Eighth => 8.0,
            GridDivision.Sixteenth => 16.0,
            GridDivision.ThirtySecond => 32.0,
            GridDivision.HalfTriplet => 3.0,
            GridDivision.QuarterTriplet => 6.0,
            GridDivision.EighthTriplet => 12.0,
            GridDivision.SixteenthTriplet => 24.0,
            GridDivision.DoubleWhole => 0.5,
            _ => 4.0,
        };
    }
}