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
        private readonly Evolution<RhythmPattern> _evolution;
        private readonly EvolutionContext _evolutionContext;
        private readonly IMutator<RhythmPattern> _mutator;
        private readonly IFitnessServiceOptions _fitnessOptions;
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

        public FitnessServiceOptions? FitnessOptions => _fitnessOptions as FitnessServiceOptions;

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

        private double _mutationRate;

        public double MutationRate
        {
            get => _mutationRate;
            set
            {
                if (SetProperty(ref _mutationRate, value))
                {
                    _evolutionContext.MutationRate = value / 100.0;
                    _eventAggregator.GetEvent<EvolutionContextChangedEvent>().Publish(_evolutionContext);
                }
            }
        }

        private double _deletionRate;
        public double DeletionRate
        {
            get => _deletionRate;
            set
            {
                if (SetProperty(ref _deletionRate, value))
                {
                    _evolutionContext.DeletionRate = value / 100.0;
                    _eventAggregator.GetEvent<EvolutionContextChangedEvent>().Publish(_evolutionContext);
                }
            }
        }

        private double _insertionRate;
        public double InsertionRate
        {
            get => _insertionRate;
            set
            {
                if (SetProperty(ref _insertionRate, value))
                {
                    _evolutionContext.InsertionRate = value / 100.0;
                    _eventAggregator.GetEvent<EvolutionContextChangedEvent>().Publish(_evolutionContext);
                }
            }
        }

        private double _crossoverRate;
        public double CrossoverRate
        {
            get => _crossoverRate;
            set
            {
                if (SetProperty(ref _crossoverRate, value))
                {
                    _evolutionContext.CrossoverRate = value / 100.0;
                    _eventAggregator.GetEvent<EvolutionContextChangedEvent>().Publish(_evolutionContext);
                }
            }
        }

        private double _swapRate;

        public double SwapRate
        {
            get => _swapRate;
            set
            {
                if (SetProperty(ref _swapRate, value))
                {
                    _evolutionContext.SwapRate = value / 100.0;
                    _eventAggregator.GetEvent<EvolutionContextChangedEvent>().Publish(_evolutionContext);
                }
            }
        }

        public ICommand GenerateRhythmCommand { get; }
        public ICommand StartStopEvolutionCommand { get; }

        public MainWindowViewModel(IEventAggregator eventAggregator, MidiClock midiClock, OutputDevice midiOut, IMutator<RhythmPattern> mutator, Evolution<RhythmPattern> evolution, 
            IFitnessServiceOptions fitnessOptions, IFitnessService fitnessService)
        {
            _eventAggregator = eventAggregator;
            _midiOut = midiOut;
            _mutator = mutator;
            _evolution = evolution;
            _fitnessOptions = fitnessOptions;
            _fitnessService = fitnessService;

            ((FitnessServiceOptions)fitnessOptions).PropertyChanged += (_, _) => _fitnessService.ApplyOptions();

            _evolutionContext = EvolutionContext.Create();

            _mutationRate = _evolutionContext.MutationRate * 100.0;
            _deletionRate = _evolutionContext.DeletionRate * 100.0;
            _insertionRate = _evolutionContext.InsertionRate * 100.0;
            _crossoverRate = _evolutionContext.CrossoverRate * 100.0;
            _swapRate = _evolutionContext.SwapRate * 100.0;

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
                    rhythm.StartEvolution(_evolutionContext);
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

        private void GenerateRhythm()
        {
            var last = Rhythms.LastOrDefault();
            var pattern = SelectedGenerator.Generate();
            if (last != null && last.WaitingForTarget)
            {
                last.SetTarget(pattern);
            }
            else
            {
                var nextChannel = GetFreeChannel();
                if (nextChannel == null) return;
                Rhythms.Add(new RhythmViewModel(pattern, nextChannel.Value, _eventAggregator, _midiOut, _evolution, _mutator, _fitnessService));
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
