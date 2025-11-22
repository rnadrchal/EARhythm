using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Egami.Rhythm.Midi;
using EnvironmentalSequencer.Models;
using EnvironmentalSequencer.Services;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using Prism.Mvvm;
using Syncfusion.Windows.Shared;

namespace EnvironmentalSequencer.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly SensorService _sensorService;
        private readonly SensorDataFactory _sensorDataFactory;

        private ulong _tickCount = 0;

        private string _title = "Environment";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        private bool _ledBeat;

        public bool LedBeat
        {
            get => _ledBeat;
            set => SetProperty(ref _ledBeat, value);
        }

        public ObservableCollection<Sensor> Sensors { get; private set; } = new();

        public ICommand RefreshSensorsCommand { get; private set; }

        public MainWindowViewModel(SensorService sensorService, SensorDataFactory sensorDataFactory)
        {
            _sensorService = sensorService;
            _sensorDataFactory = sensorDataFactory;

            _sensorService.DeviceAdded += OnDeviceAdded;
            MidiDevices.Input.EventReceived += OnClockEventReceived;
            RefreshSensorsCommand = new DelegateCommand(_ => RefreshSensors());
        }

        private void OnClockEventReceived(object? sender, MidiEventReceivedEventArgs e)
        {
            if (e.Event.EventType is MidiEventType.Start)
            {
                _tickCount = 0;
            }

            if (e.Event.EventType is MidiEventType.Stop)
            {
                LedBeat = false;
            }

            if (e.Event.EventType is MidiEventType.TimingClock)
            {
                if (_tickCount % 24 == 0)
                {
                    LedBeat = true;
                }

                if (_tickCount % 24 == 12)
                {
                    LedBeat = false;
                }
                _tickCount++;
            }
        }

        private void OnDeviceAdded(object? sender, EventArgs e)
        {
            RefreshSensors();
        }

        private void RefreshSensors()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var device in _sensorService.GetKnownDevices().Where(d => Sensors.All(s => s.Id != d.Id)))
                {
                    var sensor = new Sensor(device.Id, device.Name, device.Capabilities, _sensorService, _sensorDataFactory);
                    Sensors.Add(sensor);
                }
            });
        }
    }
}
