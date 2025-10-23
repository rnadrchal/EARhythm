using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Egami.EA.Metrics;
using Egami.Rhythm.EA;
using Egami.Rhythm.EA.Extensions;
using Egami.Rhythm.EA.Mutation;
using Egami.Rhythm.Midi;
using Egami.Rhythm.Pattern;
using EuclidEA.Events;
using EuclidEA.Services;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;

namespace EuclidEA.ViewModels;

public class RhythmViewModel : BindableBase
{
    private readonly Evolution<Sequence> _evolution;
    private readonly IEvolutionOptions _evolutionOptions;
    private readonly IFitnessService _fitnessService;
    private Sequence _sequence;
    private readonly IEventAggregator _eventAggregator;
    private Sequence? _target;
    private int _currentStep;
    private ulong _currentTick;
    private ulong _nextTick;
    private byte? _lastNote;
    private byte _channel;
    private bool _isEvolutionInProgress;
    private Population<Sequence>? _population;
    private readonly IMutator<Sequence> _mutator;

    private List<StepViewModel> _steps = new();
    public List<StepViewModel> Steps
    {
        get => _steps;
        set => SetProperty(ref _steps, value);
    }

    private List<StepViewModel>? _targetSteps;

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
        IFitnessService fitnessService, IEvolutionOptions evolutionOptions)
    {
        _sequence = sequence;
        _channel = channel;
        _eventAggregator = eventAggregator;
        _evolution = evolution;
        _mutator = mutator;
        _fitnessService = fitnessService;
        _evolutionOptions = evolutionOptions;
        Steps = sequence.Steps.Select(s => new StepViewModel
        {
            IsHit = s.Hit,
            Velocity = s.Velocity / 4,
            Pitch = s.Pitch,
            Length = Math.Max(1, s.Length)
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

        if (TargetSteps != null)
        {
            TargetSteps.Clear();
            TargetSteps = null;
            _target = null;
            RaisePropertyChanged(nameof(WaitingForTarget));
        }
        else
        {
            _eventAggregator.GetEvent<DeleteRhythmEvent>().Publish(this);
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

                _nextTick = _currentTick + (ulong)_sequence.Steps[_currentStep].Length;
                _currentStep++;
                if (_currentStep == _sequence.StepsTotal) _currentStep = 0;

                if (_isEvolutionInProgress)
                {
                    bool updateReqired = false;
                    if (_currentStep == 0)
                    {
                        var populationByFitness = _population.Individuals.OrderBy(Fitness).ToList();
                        // Evolve least fittest half of population
                        for (var i = 0; i < populationByFitness.Count / 2; ++i)
                        {
                            _population.Evolve(populationByFitness[i], _mutator);
                        }
                        _population?.Tournament(_mutator, Fitness, _evolutionOptions);

                        _sequence = _population?.Individuals.FindFittest(pattern => Fitness(pattern)).Individual ?? _sequence;
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
            Velocity = s.Velocity / 4,
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
            Velocity = s.Velocity / 4,
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

    private double _currentFitness;
    public double CurrentFitness
    {
        get => _currentFitness;
        set => SetProperty(ref _currentFitness, value);
    }

    private double _rhyhtmFitness;
    public double RhythmFitness
    {
        get => _rhyhtmFitness;
        set => SetProperty(ref _rhyhtmFitness, value);
    }

    private double _pitchFitness;
    public double PitchFitness
    {
        get => _pitchFitness;
        set => SetProperty(ref _pitchFitness, value);
    }

    private double _lengthFitness;
    public double LengthFitness
    {
        get => _lengthFitness;
        set => SetProperty(ref _lengthFitness, value);
    }

    private double _velocityFitness;
    public double VelocityFitness
    {
        get => _velocityFitness;
        set => SetProperty(ref _velocityFitness, value);
    }


    private double Fitness(Sequence pattern)
    {
        var sequence = new MetricsSequence(
            _sequence.Hits,
            _sequence.Steps.Select(s => s.Pitch).ToArray(),
            _sequence.Steps.Select(s => s.Velocity).ToArray().Select(v => v).ToArray(),
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