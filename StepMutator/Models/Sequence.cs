using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Egami.Rhythm.Common;
using Egami.Rhythm.Midi;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using Prism.Mvvm;
using Syncfusion.Windows.Shared;

namespace StepMutator.Models;

public class Sequence : BindableBase, ISequence
{

    private ulong[] _steps;

    private ulong _tickCount = 0;
    private int _currentStep = 0;
    private byte? _lastNote = null;

    private byte _channel;

    public byte Channel
    {
        get => _channel;
        set => SetProperty(ref _channel, value);
    }

    private int _divider = 16;

    public int Divider
    {
        get => _divider;
        set => SetProperty(ref _divider, value);
    }

    private byte _controlNumber = 1;

    public byte ControlNumber
    {
        get => _controlNumber;
        set => SetProperty(ref _controlNumber, value);
    }

    public ObservableCollection<ExtendedNote> Notes { get; } = new();

    public ICommand DyeCommand { get; }

    public Sequence(int length = 16)
    {
        Dye(length);
        DyeCommand = new DelegateCommand(_ => Dye(Length));
        MidiDevices.Input.EventReceived += OnMidiEvent;
    }

    private void OnMidiEvent(object? sender, MidiEventReceivedEventArgs e)
    {
        if (e.Event is StartEvent)
        {
            _tickCount = 0;
            _currentStep = 0;
        }

        if (e.Event is StopEvent)
        {
            if (_lastNote.HasValue)
            {
                MidiDevices.Output.SendEvent(new NoteOffEvent((SevenBitNumber)_lastNote.Value,
                    (SevenBitNumber)0)
                {
                    Channel = (FourBitNumber)_channel
                });
            }
        }
        if (e.Event is TimingClockEvent)
        {
            if (_tickCount % (ulong)(96 / _divider) == 0)
            {
                var step = new Step(_steps[_currentStep]);
                MidiDevices.Output.SendEvent(new PitchBendEvent(step.Pitchbend));
                MidiDevices.Output.SendEvent(new ControlChangeEvent((SevenBitNumber)_controlNumber,
                    (SevenBitNumber)step.ModWheel));
                if (!step.Tie)
                {
                    if (_lastNote.HasValue)
                    {
                        MidiDevices.Output.SendEvent(new NoteOffEvent((SevenBitNumber)_lastNote.Value,
                            (SevenBitNumber)0)
                        {
                            Channel = (FourBitNumber)_channel
                        });
                        _lastNote = null;
                    }
                }
                if (step.On)
                {
                    MidiDevices.Output.SendEvent(new NoteOnEvent((SevenBitNumber)step.Pitch,
                        (SevenBitNumber)step.Velocity)
                    {
                        Channel = (FourBitNumber)_channel
                    });
                    _lastNote = step.Pitch;
                }
                _currentStep = (_currentStep + 1) % _steps.Length;
            }
            _tickCount++;
        }
    }

    public int Length
    {
        get => _steps.Length;
        set
        {
            if (value > _steps.Length)
            {
                var steps = _steps.ToList();
                steps.AddRange(Enumerable.Range(0, value - _steps.Length).Select(_ => GetRandomStep()));
                _steps = steps.ToArray();
                RaisePropertyChanged(nameof(Steps));
                SetNotes();
            }
            if (value < _steps.Length)
            {
                _steps = _steps.Take(value).ToArray();
                if (_currentStep > _steps.Length)
                {
                    _currentStep = _steps.Length - 1;
                }
                RaisePropertyChanged(nameof(Steps));
                SetNotes();
            }
            RaisePropertyChanged(nameof(Length));
        }
    }
    public IEnumerable<IStep> Steps => _steps.Select(s => new Step(s));

    private void GenerateRandomSteps(int count)
    {
        _steps = Enumerable.Range(0, count).Select(_ => GetRandomStep()).ToArray();
        RaisePropertyChanged(nameof(Steps));
        SetNotes();
    }

    private ulong GetRandomStep()
    {
        var u1 = RandomProvider.Get(null).NextInt64(long.MinValue, long.MaxValue);
        var u2 = RandomProvider.Get(null).NextInt64(long.MinValue, long.MaxValue);
        return (ulong)(u1 ^ u2 << 32);
    }

    public void SetNotes()
    {
        Notes.Clear();
        int i = 0;
        while (i < _steps.Length)
        {
            var step = new Step(_steps[i]);
            if (step.On)
            {
                // Starte neue Note
                int length = 1;
                var pitch = step.Pitch;
                var velocity = step.Velocity;
                var pitchbend = step.Pitchbend;
                var modWheel = step.ModWheel;

                // Suche nach Ties
                int j = i + 1;
                while (j < _steps.Length)
                {
                    var nextStep = new Step(_steps[j]);
                    if (nextStep.On && nextStep.Tie && nextStep.Pitch == pitch)
                    {
                        length++;
                        j++;
                    }
                    else
                    {
                        break;
                    }
                }

                // Füge Note hinzu
                Notes.Add(new ExtendedNote(
                    false,
                    pitch,
                    velocity,
                    length,
                    (pitchbend - 8192) / 8192.0 * 50,
                    modWheel
                ));

                i += length;
            }
            else
            {
                // Zähle Pausen
                int pauseLength = 1;
                int j = i + 1;
                while (j < _steps.Length && !new Step(_steps[j]).On)
                {
                    pauseLength++;
                    j++;
                }
                Notes.Add(new ExtendedNote(true, 0, 0, pauseLength, 0, 0));
                i += pauseLength;
            }
        }
    }

    private void Dye(int length)
    {
        GenerateRandomSteps(length);
    }
}