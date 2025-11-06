using System.Windows.Input;
using Egami.Rhythm.Midi;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using Prism.Events;
using Prism.Mvvm;
using StepMutator.Models;
using StepMutator.Models.Evolution;
using StepMutator.Services;

namespace StepMutator.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IEvolutionOptions _evolutionOptions;
        private readonly IMutator<ulong> _mutator;
        private readonly FitnessSettings _fitnessSettings;
        private ulong _tick = 0;
        private string _title = "Helix";
        private int _currentStep = 0;
        private ulong _nextTick = 0;
        private byte? _lastNote;

        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        private int _divider = 16;

        public int Divider
        {
            get => _divider;
            set => SetProperty(ref _divider, value);
        }

        private byte _channel = 0;

        public byte Channel
        {
            get => _channel;
            set => SetProperty(ref _channel, value);
        }

        private Sequence _sequence;

        public Sequence Sequence
        {
            get { return _sequence; }
            set { SetProperty(ref _sequence, value); }
        }

        private bool _ledOn = false;
        public bool LedOn
        {
            get => _ledOn;
            set => SetProperty(ref _ledOn, value);
        }

        private bool _startStopPending;

        private bool _isPlaying = false;
        public bool IsPlaying
        {
            get => _isPlaying;
            set => SetProperty(ref _isPlaying, value);
        }

        public ICommand ToggleStartStopCommand { get; }

        public MainWindowViewModel(IEventAggregator eventAggregator, IEvolutionOptions evolutionOptions, IMutator<ulong> mutator, FitnessSettings fitnessSettings)
        {
            _eventAggregator = eventAggregator;
            _evolutionOptions = evolutionOptions;
            _mutator = mutator;
            _fitnessSettings = fitnessSettings;
            _sequence = new Sequence(evolutionOptions, mutator, _eventAggregator, _fitnessSettings, 16);
            MidiDevices.Input.EventReceived += OnMidiEventReceived;
            ToggleStartStopCommand = new Prism.Commands.DelegateCommand(() =>
            {
                _startStopPending = true;
            });
        }

        private void OnMidiEventReceived(object? sender, MidiEventReceivedEventArgs e)
        {
            if (e.Event is TimingClockEvent clock)
            {
                if (_tick % 24 == 0)
                {
                    LedOn = true;
                }
                else if (_tick % 24 == 12)
                {
                    LedOn = false;
                }

                ++_tick;
            }
        }

    }
}
