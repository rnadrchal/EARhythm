using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Windows.Documents;
using System.Windows.Input;
using Egami.Rhythm.Generation;
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

        public MainWindowViewModel(IEventAggregator eventAggregator, MidiClock midiClock, OutputDevice midiOut)
        {
            _eventAggregator = eventAggregator;
            _midiOut = midiOut;

            _eventAggregator.GetEvent<BeatEvent>().Subscribe(() => LedOn = true);
            _eventAggregator.GetEvent<OffBeatEvent>().Subscribe(() => LedOn = false);

            GenerateRhythmCommand = new DelegateCommand(_ => GenerateRhythm());
            StartStopEvolutionCommand = new DelegateCommand(_ => IsEvolutionInProgress = !IsEvolutionInProgress);

            _eventAggregator.GetEvent<DeleteRhythmEvent>().Subscribe(rhythm =>
            {
                Rhythms.Remove(rhythm);
            });
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
                Rhythms.Add(new RhythmViewModel(pattern, nextChannel.Value, _eventAggregator, _midiOut));
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
