using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
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
    private readonly Evolution<Egami.Rhythm.Pattern.Sequence> _evolution;
    private readonly IFitnessService _fitnessService;
    private Egami.Rhythm.Pattern.Sequence _sequence;
    private readonly IEventAggregator _eventAggregator;
    private Egami.Rhythm.Pattern.Sequence? _target = null;
    private int _currentStep = 0;
    private ulong _currentTick = 0;
    private ulong _nextTick = 0;
    private byte? _lastNote = null;
    private ulong _generations = 0;
    private byte _channel;
    private bool _isEvolutionInProgress = false;
    private Population<Sequence>? _population = null;
    private readonly IMutator<Sequence> _mutator;

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

    public RhythmViewModel(Sequence sequence, byte channel, IEventAggregator eventAggregator,
        Evolution<Sequence> evolution,
        IMutator<Sequence> mutator, 
        IFitnessService fitnessService)
    {
        _sequence = sequence;
        _channel = channel;
        _eventAggregator = eventAggregator;
        _evolution = evolution;
        _mutator = mutator;
        _fitnessService = fitnessService;
        Steps = sequence.Steps.Select(s => new StepViewModel
        {
            IsHit = s.Hit,
            Velocity = s.Velocity / 4,
            Pitch = s.Pitch,
            Length = Math.Max(1, s.Length)
        }).ToList();
        //Steps = Enumerable.Range(0, sequence.Hits.Length).Select(i => new StepViewModel
        //{
        //    IsHit = sequence.Hits[i],
        //    Velocity = sequence.Steps[i].Pitch / 4,
        //    Length = Math.Max(1, sequence.Steps[i].Pitch),
        //    Pitch = sequence.Steps[i].Pitch
        //}).ToList();

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

        if (TargetSteps != null)
        {
            TargetSteps.Clear();
            TargetSteps = null;
            _target = null;
            RaisePropertyChanged(nameof(WaitingForTarget));
        }
        else
        {
            _eventAggregator.GetEvent<Events.DeleteRhythmEvent>().Publish(this);
        }
    }

    private void OnTick(ulong tick)
    {
        if (tick % (ulong)(96 / Rate) == 0)
        {
            if (_currentTick >= _nextTick)
            {
                if (_currentStep >= _sequence.StepsTotal) _currentStep = 0;
                if (_lastNote.HasValue)
                {
                    MidiDevices.Output.SendEvent(new NoteOffEvent((SevenBitNumber)_lastNote.Value, (SevenBitNumber)0)
                        { Channel = (FourBitNumber)_channel });
                }

                if (_sequence.Steps[_currentStep].Hit)
                {
                    MidiDevices.Output.SendEvent(new NoteOnEvent((SevenBitNumber)_sequence.Steps[_currentStep].Pitch,
                        (SevenBitNumber)_sequence.Steps[_currentStep].Velocity) { Channel = (FourBitNumber)_channel });
                    _lastNote = (byte)_sequence.Steps[_currentStep].Pitch;
                }

                _nextTick = _currentTick + (ulong)_sequence.Steps[_currentStep].Pitch;
                _currentStep++;
                if (_currentStep == _sequence.StepsTotal) _currentStep = 0;

                if (_isEvolutionInProgress)
                {
                    bool updateReqired = false;
                    if (_currentStep == 0)
                    {
                        _population?.Evolve(_mutator, 1);
                        _sequence = _population?.Individuals.FindFittest(pattern => Fitness(pattern)).Individual ?? _sequence;
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
                        UpdateSteps(_sequence);
                    }
                }
            }

            _currentTick++;
        }
    }

    private void UpdateSteps(Sequence sequence)
    {
        Steps = sequence.Steps.Select(s => new StepViewModel
        {
            IsHit = s.Hit,
            Velocity = s.Velocity,
            Length = s.Length,
            Pitch = s.Pitch,
        }).ToList();
    }

    private MetricsSequence _targetMetricsSequence;

    public void SetTarget(Sequence sequence)
    {
        _target = sequence;
        TargetSteps = sequence.Steps.Select(s => new StepViewModel
        {
            IsHit = s.Hit,
            Velocity = s.Velocity,
            Length = s.Length,
            Pitch = s.Pitch,
        }).ToList();

        _population = _evolution.AddPopulation(_sequence);

        _targetMetricsSequence = new MetricsSequence(
            sequence.Hits,
            sequence.Steps.Select(p => p.Pitch).ToArray(),
            sequence.Steps.Select(s => s.Velocity).ToArray(),
            sequence.Steps.Select(s => s.Length).ToArray());

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


    private double Fitness(Egami.Rhythm.Pattern.Sequence pattern)
    {
        var sequence = new Egami.EA.Metrics.MetricsSequence(
            _sequence.Hits,
            _sequence.Steps.Select(s => s.Pitch).ToArray(),
            _sequence.Steps.Select(s => s.Length).ToArray().Select(v => (int)v).ToArray(),
            _sequence.Steps.Select(s => s.Length).ToArray());
        var breakdown = _fitnessService.EvaluateDetailed(sequence, _targetMetricsSequence);
        RhythmFitness = breakdown.Hits;
        PitchFitness = breakdown.Pitch;
        LengthFitness = breakdown.Length;
        VelocityFitness = breakdown.Velocity;
        CurrentFitness = breakdown.Total;
        return _currentFitness;
    }
}