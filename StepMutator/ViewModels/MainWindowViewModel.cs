using Egami.Rhythm.Midi;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using Prism.Mvvm;
using StepMutator.Models;

namespace StepMutator.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private ulong _tick = 0;
        private string _title = "Step Mutator";
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

        private Sequence _sequence = new Sequence(16);
        public Sequence Sequence
        {
            get { return _sequence; }
            set { SetProperty(ref _sequence, value); }
        }

        public MainWindowViewModel()
        {
            MidiDevices.Input.EventReceived += OnMidiEventReceived;
            MidiDevices.Input.StartEventsListening();
        }

        private void OnMidiEventReceived(object? sender, MidiEventReceivedEventArgs e)
        {
            if (e.Event is TimingClockEvent clock)
            {
                
                ++_tick;
            }
        }
    }
}
