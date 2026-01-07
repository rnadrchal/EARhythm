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
            BaseVelocity = 90,
            FilterCcNumber = 11,
            FilterFricativeValue = (SevenBitNumber)_filterFrictionValue,
            FilterNeutralValue = (SevenBitNumber)_filterNeutralValue,
            TrillIntervalSemitones = _trillInterval,
            TrillFraction = 1f / ((2f * _trillCount) + 1f)
        });
        _sequence = builder.BuildFromIpa(string.Join(" ", _words));
        _player.SetSequence(_sequence);
    }
}