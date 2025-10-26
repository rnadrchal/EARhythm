using System.Threading.Channels;
using System.Windows.Input;
using Egami.Rhythm.Midi;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using Prism.Events;
using Prism.Mvvm;
using StepMutator.Events;
using StepMutator.Models;

namespace StepMutator.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;
        private ulong _tick = 0;
        private string _title = "Step Mutator";
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

        public MainWindowViewModel(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
            _sequence = new Sequence(_eventAggregator, 16);
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
                if (_tick % (ulong)(96 / _divider) == 0)
                {
                    if (_startStopPending)
                    {
                        IsPlaying = !IsPlaying;
                        _startStopPending = false;
                        _eventAggregator.GetEvent<StartStopEvent>().Publish(_isPlaying);
                    }
                    if (_isPlaying)
                    {
                        HandleTick();
                    }

                }

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

        private void HandleTick()
        {
            var note = Sequence.Notes[_currentStep];
            if (_tick >= _nextTick)
            {
                if (_lastNote.HasValue)
                {
                    MidiDevices.Output.SendEvent(
                        new NoteOffEvent((SevenBitNumber)_lastNote.Value,
                            (SevenBitNumber)0)
                        {
                            Channel = (FourBitNumber)_channel
                        });
                    _lastNote = null;
                }

                if (!note.Pause)
                {
                    MidiDevices.Output.SendEvent(
                        new NoteOnEvent((SevenBitNumber)note.Pitch,
                            (SevenBitNumber)note.Velocity)
                        {
                            Channel = (FourBitNumber)_channel
                        });
                    _lastNote = note.Pitch;
                }

                _nextTick = (_tick + (ulong)(note.Length * _divider));
                _currentStep++;
                if (_currentStep >= Sequence.Notes.Count) _currentStep = 0;
            }
        }
    }
}
