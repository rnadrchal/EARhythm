using System;
using System.Linq;
using Egami.Rhythm.Midi;
using EnvironmentalSequencer.Services;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using Prism.Mvvm;

namespace EnvironmentalSequencer.Models;

public sealed class Sensor : BindableBase
{
    private readonly Guid _id;
    private readonly string _name;
    private readonly SensorService _sensorService;
    private ulong _tickCount = 0;
    private string[] _capabilities;
    private TimeSpan _timeout;

    public Guid Id => _id;
    public string Name => _name;

    public string[] Capabilities => _capabilities;

    private int[] _validDividers = new[] { 1, 2, 3, 4, 6, 8, 12, 16, 24, 32, 48, 96 };
    private int _divider = 16;

    public int Divider
    {
        get => _divider;
        set
        {
            if (_validDividers.Contains(value))
            {
                SetProperty(ref _divider, value);
            }
        }
    }

    public Sensor(Guid id, string name, string[] capabilities, SensorService sensorService)
    {
        _id = id;
        _name = name;
        _capabilities = capabilities;
        _sensorService = sensorService;

        MidiDevices.Input.EventReceived += OnClock;
    }

    private async void OnClock(object? sender, MidiEventReceivedEventArgs e)
    {
        if (e.Event.EventType is MidiEventType.Start)
        {
            _tickCount = 0;
        }

        if (e.Event.EventType is MidiEventType.TimingClock)
        {
            if (_tickCount % (ulong)(96 / _divider) == 0)
            {
                var result = await _sensorService.SendDataRequestAsync(_id, _capabilities);
                var readings = _sensorService.GetLatestReadings(_id);
            }
            _tickCount++;
        }
    }
}