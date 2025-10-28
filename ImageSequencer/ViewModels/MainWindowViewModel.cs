using System.Windows.Input;
using Egami.Rhythm.Midi;
using ImageSequencer.Models;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using Prism.Mvvm;
using Syncfusion.Windows.Shared;

namespace ImageSequencer.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly ApplicationSettings _applicationSettings;
        public ApplicationSettings ApplicationSettings => _applicationSettings;

        private readonly VisitViewer _visitViewer;
        public VisitViewer VisitViewer => _visitViewer;
        private readonly ImageViewer _imageViewer;
        public ImageViewer ImageViewer => _imageViewer;

        private string _title = "Image Sequencer";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        private bool _ledTick = false;
        public bool LedTick
        {
            get { return _ledTick; }
            set { SetProperty(ref _ledTick, value); }
        }

        public ICommand ToggleVisitCommand { get; }

        public MainWindowViewModel(ApplicationSettings applicationSettings, VisitViewer visitViewer, ImageViewer imageViewer)
        {
            _applicationSettings = applicationSettings;
            _visitViewer = visitViewer;
            _imageViewer = imageViewer;

            ToggleVisitCommand =
                new DelegateCommand(_ => ApplicationSettings.IsVisiting = !ApplicationSettings.IsVisiting);

            MidiDevices.Input.EventReceived += OnClockEvent;
        }

        private ulong _tickCount;
        private void OnClockEvent(object sender, MidiEventReceivedEventArgs e)
        {
            if (e.Event is StartEvent)
            {
                _tickCount = 0;
            }

            if (e.Event is StopEvent)
            {
                LedTick = false;
            }

            if (e.Event is TimingClockEvent)
            {
                if (_tickCount % 24 == 0)
                {
                    LedTick = true;
                }
                else if (_tickCount % 24 == 12)
                {
                    LedTick = false;
                }
                ++_tickCount;
            }
        }
    }
}
