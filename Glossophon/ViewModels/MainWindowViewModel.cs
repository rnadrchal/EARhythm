using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Egami.Phonetics.IPA;
using Egami.Rhythm.Midi;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using Prism.Commands;
using Prism.Mvvm;

namespace Glossophon.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly IpaTranscriber _ipaTranscriber;
        private readonly IpaSequencer _sequencer;
        public IpaSequencer Sequencer => _sequencer;

        private string _title = "Glossophon";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        private string _text;
        public string Text
        {
            get { return _text; }
            set { SetProperty(ref _text, value); }
        }

        private string _language = "de";

        public string Language
        {
            get => _language;
            set => SetProperty(ref _language, value);
        }

        private bool _ledOn;
        public bool LedOn
        {
            get => _ledOn;
            set => SetProperty(ref _ledOn, value);
        }

        public ICommand TranscribeCommand { get; }
        public DelegateCommand<string?> SetLanguageCommand { get; }

        public MainWindowViewModel(IpaSequencer sequencer)
        {
            _sequencer = sequencer;
            _ipaTranscriber = new IpaTranscriber();

            TranscribeCommand = new AsyncDelegateCommand(c => Transcribe(_text, c));
            SetLanguageCommand = new DelegateCommand<string?>(l => Language = l ?? "de");

            MidiDevices.Input.EventReceived += OnMidiEventReceived;
        }

        private long _tickCount = 0;

        private void OnMidiEventReceived(object sender, MidiEventReceivedEventArgs e)
        {
            if (e.Event is StartEvent)
            {
                _tickCount = 0;
            }

            if (e.Event is StopEvent)
            {
                LedOn = false;
            }

            if (e.Event is TimingClockEvent)
            {
                if (_tickCount % 24 == 0)
                {
                    LedOn = true;
                }
                else if (_tickCount % 24 == 12)
                {
                    LedOn = false;
                }
                _tickCount++;
            }
        }

        public async Task Transcribe(string text, CancellationToken cancellation)
        {
            var result = await _ipaTranscriber.ToIpaAsync(_text, _language, cancellation);
            Application.Current.Dispatcher.Invoke(() =>
            {
                _sequencer.SetText(result);
            });
        }
    }
}
