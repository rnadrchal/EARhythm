using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Documents;
using System.Windows.Input;
using Egami.Rhythm.Generation;
using EuclidEA.Models;
using EuclidEA.Services;
using Prism.Events;
using Prism.Mvvm;
using Syncfusion.Windows.Shared;

namespace EuclidEA.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;

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
            new PolyRhythmViewModel()
        };

        public ObservableCollection<RhythmViewModel> Rhythms { get; private set; } = new();

        public ICommand GenerateRhythmCommand { get; }

        public MainWindowViewModel(IEventAggregator eventAggregator, MidiClock midiClock)
        {
            _eventAggregator = eventAggregator;

            _eventAggregator.GetEvent<BeatEvent>().Subscribe(() => LedOn = true);
            _eventAggregator.GetEvent<OffBeatEvent>().Subscribe(() => LedOn = false);

            GenerateRhythmCommand = new DelegateCommand(_ => GenerateRhythm());
        }

        private void GenerateRhythm()
        {
            var pattern = SelectedGenerator.Generate();
            Rhythms.Add(new RhythmViewModel(pattern));
        }
    }
}
