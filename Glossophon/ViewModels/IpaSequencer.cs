using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Egami.Phonetics.Sequencer;
using Egami.Rhythm.Midi;
using Egami.Sequencer;
using Egami.Sequencer.Common;
using Egami.Sequencer.Grid;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Prism.Mvvm;

namespace Glossophon.ViewModels;

public sealed class IpaSequencer : BindableBase
{
    private readonly MidiClockGrid _clock;
    private readonly GridSequencePlayer _player;
    private string[] _words = [];
    public string[] Words => _words;

    private MusicalSequence _sequence = MusicalSequence.Empty;
    public MusicalSequence Sequence => _sequence;
    public IEnumerable<SequenceStep> Steps => Enumerable.OfType<SequenceStep>(_sequence.Steps);

    public GridDivision Division => (GridDivision)_gridDivisionIndex;

    private int _gridDivisionIndex = (int)GridDivision.Sixteenth;

    public int GridDivisionIndex
    {
        get => _gridDivisionIndex;
        set
        {
            if (SetProperty(ref _gridDivisionIndex, value))
            {
                RaisePropertyChanged(nameof(Division));
                _clock.SetDivision(Division);
            }
        }
    }

    private int _stepsPerUnit = 2;

    public int StepsPerUnit
    {
        get => _stepsPerUnit;
        set
        {
            if (SetProperty(ref _stepsPerUnit, value))
            {
                UpdateSequence();
            }
        }
    }

    private int _rootNote = 60;

    public int RootNote
    {
        get => _rootNote;
        set
        {
            if (SetProperty(ref _rootNote, value))
            {
                UpdateSequence();
            }
        }
    }

    public ScaleType ScaleType => (ScaleType)_scaleTypeIndex;
    private int _scaleTypeIndex = (int)Egami.Sequencer.Common.ScaleType.Major;
    public int ScaleTypeIndex
    {
        get => _scaleTypeIndex;
        set
        {
            if (SetProperty(ref _scaleTypeIndex, value))
            {
                RaisePropertyChanged(nameof(ScaleType));
                UpdateSequence();
            }
        }
    }

    private int _baseVelocity = 64;
    public int BaseVelocity
    {
        get => _baseVelocity;
        set
        {
            if (SetProperty(ref _baseVelocity, value))
            {
                UpdateSequence();
            }
        }
    }

    private int _plosiveAccentFraction = 25;
    public int PlosiveAccentFraction
    {
        get => _plosiveAccentFraction;
        set
        {
            if (SetProperty(ref _plosiveAccentFraction, value))
            {
                UpdateSequence();
            }
        }
    }

    private int _filterNeutralValue = 32;

    public int FilterNeutralValue
    {
        get => _filterNeutralValue;
        set
        {
            if (SetProperty(ref _filterNeutralValue, value))
            {
                UpdateSequence();
            }
        }
    }


    private int _filterFrictionValue = 96;

    public int FilterFrictionValue
    {
        get => _filterFrictionValue;
        set
        {
            if (SetProperty(ref _filterFrictionValue, value))
            {
                UpdateSequence();
            }
        }
    }

    private int _trillInterval = 1;

    public int TrillInterval
    {
        get => _trillInterval;
        set
        {
            if (SetProperty(ref _trillInterval, value))
            {
                UpdateSequence();
            }
        }
    }

    private int _trillCount = 1;

    public int TrillCount
    {
        get => _trillCount;
        set
        {
            if (SetProperty(ref _trillCount, value))
            {
                UpdateSequence();
            }
        }
    }

    private int _stressDurationFactor = 130;

    public int StressDurationFactor
    {
        get => _stressDurationFactor;
        set
        {
            if (SetProperty(ref _stressDurationFactor, value))
            {
                UpdateSequence();
            }
        }
    }

    private int _stressLoudnessFactor = 115;
    public int StressLoudnessFactor
    {
        get => _stressLoudnessFactor;
        set
        {
            if (SetProperty(ref _stressLoudnessFactor, value))
            {
                UpdateSequence();
            }
        }
    }

    private int _nasalTailFraction = 30;
    public int NasalTailFraction
    {
        get => _nasalTailFraction;
        set
        {
            if (SetProperty(ref _nasalTailFraction, value))
            {
                UpdateSequence();
            }
        }
    }

    private int _nasalGlideSemitones = 1;
    public int NasalGlideSemitones
    {
        get => _nasalGlideSemitones;
        set
        {
            if (SetProperty(ref _nasalGlideSemitones, value))
            {
                UpdateSequence();
            }
        }
    }

    public IpaSequencer()
    {
        _clock = new MidiClockGrid(Division);
        _player = new GridSequencePlayer(
            _sequence,
            MidiDevices.Output,
            (FourBitNumber)0,
            _clock);

        MidiDevices.Input.EventReceived += OnMidiClock;
    }

    private void OnMidiClock(object sender, Melanchall.DryWetMidi.Multimedia.MidiEventReceivedEventArgs e)
    {
        if (e.Event is StartEvent)
        {
            _player.Start();
        }

        if (e.Event is StopEvent)
        {
            _player.Stop();
        }
        if (e.Event is TimingClockEvent)
        {
            _clock.OnClockPulse();
        }
    }

    public void SetText(IEnumerable<string> ipaWords)
    {
        _words = ipaWords is null ? [] : ipaWords.ToArray();
        RaisePropertyChanged(nameof(Words));
        UpdateSequence();
    }

    private void UpdateSequence()
    {
        var builder = new PhoneticSequenceBuilder(new PhoneticSequencerSettings
        {
            RootNote = (SevenBitNumber)_rootNote,
            Scale = ScaleType,
            BaseStepsPerUnit = _stepsPerUnit,
            BaseVelocity = _baseVelocity,
            PlosiveAccentFraction = _plosiveAccentFraction / 100.0,
            FilterCcNumber = 11,
            FilterFricativeValue = (SevenBitNumber)_filterFrictionValue,
            FilterNeutralValue = (SevenBitNumber)_filterNeutralValue,
            TrillIntervalSemitones = _trillInterval,
            TrillCount = _trillCount,
            // behalte bisherigen Ansatz zur Daueraufteilung (optional anpassbar)
            TrillFraction = 1f / ((2f * _trillCount) + 1f),
            StressDurationFactor = _stressDurationFactor / 100.0,
            StressLoudnessFactor = _stressLoudnessFactor / 100.0,
            NasalGlideSemitones = _nasalGlideSemitones,
            NasalTailFraction = _nasalTailFraction / 100.0
        }); _sequence = builder.BuildFromIpa(string.Join(" ", _words));
        _player.SetSequence(_sequence);
        RaisePropertyChanged(nameof(Steps));
    }
}