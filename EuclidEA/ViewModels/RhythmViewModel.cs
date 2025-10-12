using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using Egami.Rhythm.Pattern;
using EuclidEA.Services;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Composing;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Syncfusion.Windows.Controls.Input;

namespace EuclidEA.ViewModels;

public class RhythmViewModel : BindableBase
{
    private readonly OutputDevice _midiOut;
    private readonly RhythmPattern pattern;
    private readonly IEventAggregator _eventAggregator;
    private RhythmPattern? _target = null;
    private int _currentStep = 0;
    private ulong _currentTick = 0;
    private ulong _nextTick = 0;
    private byte? _lastNote = null;
    private byte _channel;


    public List<StepViewModel> Steps { get; private set; }
    public List<StepViewModel>? _targetSteps = null;
    public List<StepViewModel>? TargetSteps
    {
        get => _targetSteps;
        set => SetProperty(ref _targetSteps, value);
    }

    public bool WaitingForTarget => _target == null;

    public int[] Rates => new [] { 32, 24, 16, 12, 8, 6, 4, 3, 2, 1 };
    private int _rate = 2;
    public int RateIndex
    {
        get => _rate;
        set
        {
            if (SetProperty(ref _rate, value))
            {
                RaisePropertyChanged(nameof(Rate));
            }

        }
    }

    public int Rate => Rates[RateIndex];

    public byte Channel => _channel;

    public ICommand DeleteMeCommand { get; } 

    public RhythmViewModel(RhythmPattern pattern, byte channel, IEventAggregator eventAggregator, OutputDevice midiOut)
    {
        this.pattern = pattern;
        _channel = channel;
        _eventAggregator = eventAggregator;
        _midiOut = midiOut;
        Steps = Enumerable.Range(0, pattern.Hits.Length).Select(i => new StepViewModel
        {
            IsHit = pattern.Hits[i],
            Velocity = pattern.Velocities[i] / 4,
            Length = Math.Max(1, pattern.Lengths[i]),
            Pitch = pattern.Pitches[i]
        }).ToList();

        DeleteMeCommand = new DelegateCommand(OnDeleteMe);
        _eventAggregator.GetEvent<ClockEvent>().Subscribe(OnTick);
    }

    private void OnDeleteMe()
    {
        if (_lastNote.HasValue)
        {
            _midiOut.SendEvent(new NoteOffEvent((SevenBitNumber)_lastNote.Value, (SevenBitNumber)0) { Channel = (FourBitNumber)_channel});
        }
        _eventAggregator.GetEvent<Events.DeleteRhythmEvent>().Publish(this);
    }

    private void OnTick(ulong tick)
    {
        if (tick % (ulong)(96 / Rate) == 0)
        {
            if (_currentTick == _nextTick)
            {
                if (_lastNote.HasValue)
                {
                    _midiOut.SendEvent(new NoteOffEvent((SevenBitNumber)_lastNote.Value,  (SevenBitNumber)0) { Channel = (FourBitNumber)_channel});
                }

                if (pattern.Hits[_currentStep] && pattern.Pitches[_currentStep].HasValue)
                {
                    _midiOut.SendEvent(new NoteOnEvent((SevenBitNumber)pattern.Pitches[_currentStep], (SevenBitNumber)pattern.Velocities[_currentStep]) { Channel = (FourBitNumber)_channel});
                    _lastNote = (byte)pattern.Pitches[_currentStep];
                }

                _nextTick = _currentTick + (ulong)pattern.Lengths[_currentStep];
                _currentStep++;
                if (_currentStep == pattern.StepsTotal) _currentStep = 0;
            }

            _currentTick++;
        }
    }

    public void SetTarget(RhythmPattern target)
    {
        _target = target;
        TargetSteps = Enumerable.Range(0, target.Hits.Length).Select(i => new StepViewModel
        {
            IsHit = target.Hits[i],
            Velocity = target.Velocities[i] / 4,
            Length = Math.Max(1, target.Lengths[i]),
            Pitch = target.Pitches[i]
        }).ToList();
        RaisePropertyChanged(nameof(WaitingForTarget));
    }

}