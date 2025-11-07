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
        private ulong _tick = 0;
        private string _title = "Helix";

        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        private readonly Sequence _sequence;

        public Sequence Sequence => _sequence;

        private bool _ledOn = false;
        public bool LedOn
        {
            get => _ledOn;
            set => SetProperty(ref _ledOn, value);
        }

        public MainWindowViewModel(Sequence sequence)
        {
            _sequence = sequence;
            MidiDevices.Input.EventReceived += OnMidiEventReceived;
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
