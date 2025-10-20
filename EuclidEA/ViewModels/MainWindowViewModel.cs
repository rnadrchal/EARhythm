using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Egami.EA.Metrics;
using Egami.Rhythm.EA;
using Egami.Rhythm.EA.Mutation;
using Egami.Rhythm.Pattern;
using EuclidEA.Events;
using EuclidEA.Models;
using EuclidEA.Services;
using EuclidEA.ViewModels.Rhythm;
using Melanchall.DryWetMidi.Multimedia;
using Prism.Events;
using Prism.Mvvm;
using Syncfusion.Windows.Shared;
using MidiClock = EuclidEA.Services.MidiClock;

namespace EuclidEA.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly OutputDevice _midiOut;
        private readonly EvolutionOptions _evolutionOptions;
        private readonly Evolution<Egami.Rhythm.Pattern.Sequence> _evolution;
        private readonly IMutator<Egami.Rhythm.Pattern.Sequence> _mutator;
        private readonly FitnessServiceOptions _fitnessOptions;
        private readonly IFitnessService _fitnessService;

        private string _title = "Euclid EA";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private bool _ledOn;
        public bool LedOn
        {
            get => _ledOn;
            set => SetProperty(ref _ledOn, value);
        }

        public IRhythmGeneratorViewModel SelectedGenerator => Generators[_rhythmGeneration];

        private int _rhythmGeneration =(int) RhythmGenerationMethod.Bernoulli;
        public int RhythmGeneration

        {
            get => _rhythmGeneration;
            set
            {
                if (SetProperty(ref _rhythmGeneration, value))
                {
                    RaisePropertyChanged(nameof(SelectedGenerator));
                };

            }
        }

        public EvolutionOptions? EvolutionOptions => _evolutionOptions;

        public FitnessServiceOptions? FitnessOptions => _fitnessOptions;

        public List<IRhythmGeneratorViewModel> Generators { get; private set; } = new()
        {
            new EuclidGeneratorViewModel(),
            new BernoulliGeneratorViewModel(),
            new PoissonGeneratorViewModel(),
            new CellularAutomatonViewModel(),
            new LSystemViewModel(),
            new PolyRhythmViewModel(),
            new TrackChunkGeneratorViewModel()
        };

        public ObservableCollection<RhythmViewModel> Rhythms { get; private set; } = new();

        private bool _isEvolutionInProgress;
        public bool IsEvolutionInProgress
        {
            get => _isEvolutionInProgress;
            set => SetProperty(ref _isEvolutionInProgress, value);
        }

        public ICommand GenerateRhythmCommand { get; }
        public ICommand StartStopEvolutionCommand { get; }

        public MainWindowViewModel(IEventAggregator eventAggregator, MidiClock midiClock, OutputDevice midiOut, IMutator<Egami.Rhythm.Pattern.Sequence> mutator, Evolution<Egami.Rhythm.Pattern.Sequence> evolution, 
            IFitnessServiceOptions fitnessOptions, IFitnessService fitnessService, IEvolutionOptions evolutionOptions)
        {
            _eventAggregator = eventAggregator;
            _midiOut = midiOut;
            _mutator = mutator;
            _evolution = evolution;
            _fitnessOptions = fitnessOptions as FitnessServiceOptions ?? throw new ArgumentException(nameof(fitnessOptions));
            _fitnessService = fitnessService;
            _evolutionOptions = evolutionOptions as EvolutionOptions ?? throw new ArgumentException(nameof(evolutionOptions));

            ((FitnessServiceOptions)fitnessOptions).PropertyChanged += (_, _) => _fitnessService.ApplyOptions();
            _evolutionOptions.PropertyChanged += (_, _) => _evolution.ApplyOptions(evolutionOptions);
            _eventAggregator.GetEvent<BeatEvent>().Subscribe(() => LedOn = true);
            _eventAggregator.GetEvent<OffBeatEvent>().Subscribe(() => LedOn = false);

            GenerateRhythmCommand = new DelegateCommand(_ => GenerateRhythm());
            StartStopEvolutionCommand = new DelegateCommand(OnEvolutionStartStop);

            _eventAggregator.GetEvent<DeleteRhythmEvent>().Subscribe(rhythm =>
            {
                Rhythms.Remove(rhythm);
            });
        }

        private void OnEvolutionStartStop(object obj)
        {
            IsEvolutionInProgress = !IsEvolutionInProgress;
            if (_isEvolutionInProgress)
            {
                foreach (var rhythm in Rhythms)
                {
                    rhythm.StartEvolution();
                }
            }
            else
            {
                foreach (var rhythm in Rhythms)
                {
                    rhythm.PauseEvolution();
                }
            }
        }

        private RhythmViewModel _lastAddedRhythm;

        private void GenerateRhythm()
        {
            var pattern = SelectedGenerator.Generate();
            var last = Rhythms.FirstOrDefault(r => r.TargetSteps == null);
            if (last is { WaitingForTarget: true })
            {
                last.SetTarget(pattern);
                if (_isEvolutionInProgress) _lastAddedRhythm.StartEvolution();
            }
            else
            {
                var nextChannel = GetFreeChannel();
                _lastAddedRhythm = new RhythmViewModel(pattern, nextChannel.Value, _eventAggregator, _evolution,
                    _mutator, _fitnessService);
                Rhythms.Add(_lastAddedRhythm);
            }
        }

        private byte? GetFreeChannel()
        {
            byte channel = 0;
            var occupiedChannels = Rhythms.Select(r => r.Channel).ToList();
            return (byte?)Enumerable.Range(0, 16).FirstOrDefault(c => !occupiedChannels.Contains((byte)c));
        }
    }
}
