using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using Egami.EA.Metrics;
using Egami.Rhythm.EA;
using Egami.Rhythm.EA.Mutation;
using Egami.Rhythm.Extensions;
using Egami.Rhythm.Pattern;
using EuclidEA.Events;
using EuclidEA.Services;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Composing;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Syncfusion.Windows.Controls.Input;
using Egami.Rhythm.Extensions;
using Egami.Rhythm.EA.Extensions;
using Egami.Pitch;
using Egami.Rhythm.Midi;
using Microsoft.Extensions.FileSystemGlobbing.Internal;

namespace EuclidEA.ViewModels;

public class RhythmViewModel : BindableBase
{
    private readonly Evolution<RhythmPattern> _evolution;
    private readonly IFitnessService _fitnessService;
    private RhythmPattern _pattern;
    private readonly IEventAggregator _eventAggregator;
    private RhythmPattern? _target = null;
    private int _currentStep = 0;
    private ulong _currentTick = 0;
    private ulong _nextTick = 0;
    private byte? _lastNote = null;
    private ulong _generations = 0;
    private byte _channel;
    private bool _isEvolutionInProgress = false;
    private Population<RhythmPattern>? _population = null;
    private readonly IMutator<RhythmPattern> _mutator;

    private List<StepViewModel> _steps = new();
    public List<StepViewModel> Steps
    {
        get => _steps;
        set => SetProperty(ref _steps, value);
    }

    private List<StepViewModel>? _targetSteps = null;

    public List<StepViewModel>? TargetSteps
    {
        get => _targetSteps;
        set => SetProperty(ref _targetSteps, value);
    }

    public bool WaitingForTarget => _target == null;

    public int[] Rates => new[] { 32, 24, 16, 12, 8, 6, 4, 3, 2, 1 };
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

    public RhythmViewModel(RhythmPattern pattern, byte channel, IEventAggregator eventAggregator,
        Evolution<RhythmPattern> evolution,
        IMutator<RhythmPattern> mutator, 
        IFitnessService fitnessService)
    {
        this._pattern = pattern;
        _channel = channel;
        _eventAggregator = eventAggregator;
        _evolution = evolution;
        _mutator = mutator;
        _fitnessService = fitnessService;
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

    public void StartEvolution()
    {
        _isEvolutionInProgress = true;
    }

    public void PauseEvolution()
    {
        _isEvolutionInProgress = false;
    }

    private void OnDeleteMe()
    {
        if (_lastNote.HasValue)
        {
            MidiDevices.Output.SendEvent(new NoteOffEvent((SevenBitNumber)_lastNote.Value, (SevenBitNumber)0)
                { Channel = (FourBitNumber)_channel });
        }

        _eventAggregator.GetEvent<Events.DeleteRhythmEvent>().Publish(this);
    }

    private void OnTick(ulong tick)
    {
        if (tick % (ulong)(96 / Rate) == 0)
        {
            if (_currentTick >= _nextTick)
            {
                if (_currentStep >= _pattern.StepsTotal) _currentStep = 0;
                if (_lastNote.HasValue)
                {
                    MidiDevices.Output.SendEvent(new NoteOffEvent((SevenBitNumber)_lastNote.Value, (SevenBitNumber)0)
                        { Channel = (FourBitNumber)_channel });
                }

                if (_pattern.Hits[_currentStep] && _pattern.Pitches[_currentStep].HasValue)
                {
                    MidiDevices.Output.SendEvent(new NoteOnEvent((SevenBitNumber)_pattern.Pitches[_currentStep],
                        (SevenBitNumber)_pattern.Velocities[_currentStep]) { Channel = (FourBitNumber)_channel });
                    _lastNote = (byte)_pattern.Pitches[_currentStep];
                }

                _nextTick = _currentTick + (ulong)_pattern.Lengths[_currentStep];
                _currentStep++;
                if (_currentStep == _pattern.StepsTotal) _currentStep = 0;

                if (_isEvolutionInProgress)
                {
                    bool updateReqired = false;
                    if (_currentStep == 0)
                    {
                        _population?.Evolve(_mutator, 1);
                        _pattern = _population?.Individuals.FindFittest(pattern => Fitness(pattern)).Individual ?? _pattern;
                        _generations++;
                        updateReqired = true;
                    }

                    if (_generations % 4 == 0)
                    {
                        _population?.Pairing(_mutator, Fitness);
                        updateReqired = true;
                    }
                    if (updateReqired)
                    {
                        UpdateSteps(_pattern);
                    }
                }
            }

            _currentTick++;
        }
    }

    private void UpdateSteps(RhythmPattern pattern)
    {
        Steps = Enumerable.Range(0, pattern.Hits.Length).Select(i => new StepViewModel
        {
            IsHit = pattern.Hits[i],
            Velocity = pattern.Velocities[i] / 4,
            Length = Math.Max(1, pattern.Lengths[i]),
            Pitch = pattern.Pitches[i]
        }).ToList();
    }

    private Sequence _targetSequence;

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

        _population = _evolution.AddPopulation(_pattern);

        _targetSequence = new Sequence(
            target.Hits,
            target.Pitches.Select(p => p.HasValue ? p.Value : 0).ToArray(),
            target.Velocities.Select(v => (int)v).ToArray(),
            target.Lengths);

        RaisePropertyChanged(nameof(WaitingForTarget));
    }

    private double _currentFitness = 0;
    public double CurrentFitness
    {
        get => _currentFitness;
        set => SetProperty(ref _currentFitness, value);
    }

    private double _rhyhtmFitness = 0.0;
    public double RhythmFitness
    {
        get => _rhyhtmFitness;
        set => SetProperty(ref _rhyhtmFitness, value);
    }

    private double _pitchFitness = 0.0;
    public double PitchFitness
    {
        get => _pitchFitness;
        set => SetProperty(ref _pitchFitness, value);
    }

    private double _lengthFitness = 0.0;
    public double LengthFitness
    {
        get => _lengthFitness;
        set => SetProperty(ref _lengthFitness, value);
    }

    private double _velocityFitness = 0.0;
    public double VelocityFitness
    {
        get => _velocityFitness;
        set => SetProperty(ref _velocityFitness, value);
    }


    private double Fitness(RhythmPattern pattern)
    {
        var sequence = new Sequence(
            _pattern.Hits,
            _pattern.Pitches.Select(p => p.HasValue ? p.Value : 0).ToArray(), 
            _pattern.Velocities.Select(v => (int)v).ToArray(),
            _pattern.Lengths);
        var breakdown = _fitnessService.EvaluateDetailed(sequence, _targetSequence);
        RhythmFitness = breakdown.Hits;
        PitchFitness = breakdown.Pitch;
        LengthFitness = breakdown.Length;
        VelocityFitness = breakdown.Velocity;
        CurrentFitness = breakdown.Total;
        return _currentFitness;
    }
}