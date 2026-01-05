using Microsoft.Win32;
using Prism.Mvvm;
using Syncfusion.Windows.Shared;
using System.Threading.Tasks;
using System.Windows.Input;
using Egami.Rhythm.Midi;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;

namespace Blattwerk.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private long _tickCount = 0;
        private readonly ImageConfiguration _imageConfig;
        public ImageConfiguration ImageConfig => _imageConfig;

        private string _title = "BLATTWERK";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        private bool _ledOn = false;
        public bool LedOn
        {
            get => _ledOn;
            set => SetProperty(ref _ledOn, value);
        }

        public ICommand LoadImageCommand { get; }

        public MainWindowViewModel(ImageConfiguration imageConfig)
        {
            _imageConfig = imageConfig;
            LoadImageCommand = new DelegateCommand(_ => LoadImage());

            MidiDevices.Input.EventReceived += OnMidiEventReceived;
        }

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
                ++_tickCount;
            }
        }

        private async Task LoadImage()
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter =
                    "Images (*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tiff)|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tiff|All Files (*.*)|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                await _imageConfig.LoadImageAsync(dlg.FileName);
            }
        }

    }
}
