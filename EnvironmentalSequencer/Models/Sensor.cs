using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Egami.Rhythm.Midi;
using EnvironmentalSequencer.Services;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using Prism.Mvvm;
using Syncfusion.Windows.Shared;

namespace EnvironmentalSequencer.Models;

public sealed class Sensor : BindableBase
{
    private readonly SensorDataFactory _sensorDataFactory;
    private readonly Guid _id;
    private readonly string _name;
    private readonly SensorService _sensorService;
    private ulong _tickCount = 0;
    private string[] _capabilities;
    private TimeSpan _timeout;

    public Guid Id => _id;
    public string Name => _name;

    public string[] Capabilities => _capabilities;

    private int _channel = 0;
    public int Channel
    {
        get => _channel;
        set => SetProperty(ref _channel, value);
    }

    private int _velocity = 60;
    public int Velocity
    {
        get => _velocity;
        set => SetProperty(ref _velocity, value);
    }

    private int _maxChordSize = 4;
    public int MaxChordSize
    {
        get => _maxChordSize;
        set => SetProperty(ref _maxChordSize, value);
    }

    private int[] _validDividers = new[] { 1, 2, 3, 4, 6, 8, 12, 16, 24, 32, 48, 96 };

    private int _divider = 4;

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

    private bool _legato = true;

    public bool Legato
    {
        get => _legato;
        set => SetProperty(ref _legato, value);
    }

    private ISensorData _sensorData = null!;
    public ISensorData SensorData
    {
        get => _sensorData;
        set => SetProperty(ref _sensorData, value);
    }

    private bool _ledPitch;

    public bool LedPitch
    {
        get => _ledPitch;
        set => SetProperty(ref _ledPitch, value);
    }

    public ICommand ToggleLegatoCommand { get; }
    public ICommand ResetOutputCommand { get; }

    public Sensor(Guid id, string name, string[] capabilities, SensorService sensorService, SensorDataFactory sensorDataFactory)
    {
        _id = id;
        _name = name;
        _capabilities = capabilities;
        _sensorService = sensorService;
        _sensorDataFactory = sensorDataFactory;

        MidiDevices.Input.EventReceived += OnClock;

        ToggleLegatoCommand = new DelegateCommand(_ => Legato = !Legato);
        ResetOutputCommand = new DelegateCommand(_ => SendNoteOff());
    }

    private HashSet<byte> _activeNotes = new();

    private async void OnClock(object? sender, MidiEventReceivedEventArgs e)
    {
        if (e.Event.EventType is MidiEventType.Start)
        {
            _tickCount = 0;
        }

        if (e.Event.EventType is MidiEventType.Stop)
        {
            SendNoteOff();
        }

        if (e.Event.EventType is MidiEventType.TimingClock)
        {
            if (_tickCount % (ulong)(96 / _divider) == 0)
            {
                var result = await _sensorService.SendDataRequestAsync(_id, _capabilities);
                var readings = _sensorService.GetLatestReadings(_id) ?? new List<SensorReading>();
                SensorData = _sensorDataFactory.GetOrCreate(_name, readings!);
                SendMidiEvents(SensorData.GetAllMappings());
            }

            if (_tickCount % (ulong)(96 / _divider) == (ulong)Math.Max(1, 48 / _divider))
            {
                if (LedPitch)
                {
                    LedPitch = false;
                }
            }

            _tickCount++;
        }
    }

    private void SendMidiEvents(IEnumerable<ValueMapping> mappings)
    {
        var velocity = mappings.Where(m => m.Target == TargetValue.Velocity).MaxBy(m => m.ToByte())?.ToByte() ?? (byte)_velocity;

        TargetValue[] sendTypes = new[]
        {
            TargetValue.Pitch,
            TargetValue.ControlChange,
            TargetValue.PitchBend
        };

        foreach (var mapping in mappings.Where(m => sendTypes.Contains(m.Target)).OrderByDescending(m => m.Target))
        {
            switch (mapping.Target)
            {
                case TargetValue.Pitch:
                    SendNoteOn(mapping.ToByte(), velocity);
                    break;
                case TargetValue.ControlChange:
                    SendControlChange(mapping.ToByte());
                    break;
                case TargetValue.PitchBend:
                    SendPitchBend(mapping.ToUshort());
                    break;
            }
        }

        while (_activeNotes.Count > _maxChordSize)
        {
            SendNoteOff(_activeNotes.First());
        }
    }

    public void SendNoteOn(byte pitch, byte velocity)
    {
        if (!Legato && _activeNotes.Contains(pitch))
        {
            SendNoteOff(pitch);
        }
        if (Legato && _activeNotes.Contains(pitch))
        {
            return;
        }
        MidiDevices.Output.SendEvent(new NoteOnEvent((SevenBitNumber)pitch, (SevenBitNumber)velocity) { Channel = (FourBitNumber)_channel });
        if (!_activeNotes.Contains(pitch))
        {
            _activeNotes.Add(pitch);
            if (!LedPitch)
            {
                LedPitch = true;
            }

        }
    }

    private void SendNoteOff(byte pitch)
    {
        MidiDevices.Output.SendEvent(new NoteOffEvent((SevenBitNumber)pitch, (SevenBitNumber)0) { Channel = (FourBitNumber)_channel });
        if (_activeNotes.Contains(pitch))
        {
            _activeNotes.Remove(pitch);
        }
    }

    private void SendNoteOff()
    {
        foreach (var pitch in _activeNotes)
        {
            SendNoteOff(pitch);
        }
    }

    private void SendControlChange(byte value, byte number = 1)
    {
        MidiDevices.Output.SendEvent(new ControlChangeEvent((SevenBitNumber)number, (SevenBitNumber)value));
    }

    private void SendPitchBend(ushort value)
    {
        MidiDevices.Output.SendEvent(new PitchBendEvent(value));
    }
}