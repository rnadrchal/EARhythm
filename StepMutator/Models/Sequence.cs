using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using Egami.Rhythm.EA.Extensions;
using Egami.Rhythm.Midi;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using Prism.Events;
using Prism.Mvvm;
using StepMutator.Common;
using StepMutator.Events;
using StepMutator.Models.Evolution;
using StepMutator.Services;
using Syncfusion.Windows.Shared;
using RandomProvider = Egami.Rhythm.Common.RandomProvider;

namespace StepMutator.Models;

public class Sequence : BindableBase, ISequence
{
    IEvolutionOptions _evolutionOptions;
    private IEventAggregator _eventAggregator;
    private IMutator<ulong> _mutator;
    private ulong[][] _steps;
    private ulong[] _attractor;

    private ulong _tickCount = 0;
    private int _currentStep = 0;
    private byte? _lastNote = null;

    private byte _channel;

    public IEvolutionOptions Options => _evolutionOptions;

    public IFitness[] Fitness { get; } =
    [
        new NoteFitness(),
        new VelocityFitness(),
        new OffFitness(),
        new TieFitness(),
        new PitchbendFitness(),
        new ControlChangeFitness()
    ];

    private bool _showSteps = false;
    public bool ShowSteps
    {
        get => _showSteps;
        set => SetProperty(ref _showSteps, value);
    }

    public byte Channel
    {
        get => _channel;
        set
        {
            if (SetProperty(ref _channel, value))
            {
                // ±0,5 Halbton: semitones = 0, fraction = 64 (≈ 50 Cent)
                SetPitchBendRange(MidiDevices.Output, _channel, 0, 64);
            }
        }
    }

    private int _divider = 16;

    private static readonly int[] ValidDividers = [1, 2, 4, 8, 12, 16, 24, 32, 48, 64, 96];
    public int Divider
    {
        get => _divider;
        set
        {
            if (!ValidDividers.Contains(value)) return;
            SetProperty(ref _divider, value);
        }
    }


    private byte _controlNumber = 1;

    public byte ControlNumber
    {
        get => _controlNumber;
        set => SetProperty(ref _controlNumber, value);
    }

    private bool _sendPitchbend;
    public bool SendPitchbend
    {
        get => _sendPitchbend;
        set => SetProperty(ref _sendPitchbend, value);
    }

    private bool _isEvolutionActive = false;
    public bool IsEvolutionActive
    {
        get => _isEvolutionActive;
        set => SetProperty(ref _isEvolutionActive, value);
    }

    private bool _isRecording;

    public bool IsRecording
    {
        get => _isRecording;
        set => SetProperty(ref _isRecording, value);
    }

    private int _recordingStep = 0;
    public int RecordingStep
    {
        get => _recordingStep;
        set => SetProperty(ref _recordingStep, value);
    }

    // Standardmäßig 14-bit center (kein Bend)
    private ushort _recordingPitchbend = PitchbendHelpers.RawCenter;

    public ushort RecordingPitchbend
    {
        get => _recordingPitchbend;
        set => SetProperty(ref _recordingPitchbend, value);
    }

    // Aktuelle Pitchbend-Range in Halbton (z.B. 0.5 = ±50 cents)
    private double _pitchbendRangeSemitones = 0.5;

    public ObservableCollection<ExtendedNote> Notes { get; } = new();
    public IEnumerable<IStep> Steps => _steps.Select(s =>
    {
        var fittest = Fittest(s);
        return new Step(fittest) { Fitness = CombinedFitness(fittest) };
    });

    public ObservableCollection<ExtendedNote> AttractorNotes { get; } = new();

    public IEnumerable<IStep> Attractor => _attractor.Select(a => new Step(a));

    public int Length
    {
        get => _steps.Length;
        set
        {
            if (value <= 0) return;
            if (value > _steps.Length)
            {
                var steps = _steps.ToList();
                var attractorSteps = _attractor.ToList();
                for (var i = 0; i < value - _steps.Length; ++i)
                {
                    steps.Add(CreateRandomPopulation());
                    attractorSteps.Add(GetRandomStep());
                }
                _steps = steps.ToArray();
                _attractor = attractorSteps.ToArray();
                RaisePropertyChanged(nameof(Steps));
                RaisePropertyChanged(nameof(Attractor));
                SetNotes();
                SetAttractorNotes();
            }
            if (value < _steps.Length)
            {
                _steps = _steps.Take(value).ToArray();
                _attractor = _attractor.Take(value).ToArray();
                if (_currentStep > _steps.Length)
                {
                    _currentStep = _steps.Length - 1;
                }
                RaisePropertyChanged(nameof(Steps));
                RaisePropertyChanged(nameof(Attractor));
                Application.Current.Dispatcher.Invoke(() =>
                {
                    SetNotes();
                    SetAttractorNotes();
                });
            }

            if (RecordingStep >= value)
            {
                RecordingStep = value - 1;
            }
            RaisePropertyChanged();
        }
    }

    public ICommand DyeCommand { get; }
    public ICommand ToggleEvolutionCommand { get; }
    public ICommand TogglePitchbendCommand { get; }
    public ICommand ToggleViewCommand { get; }
    public ICommand ToggleRecordCommand { get; }

    public Sequence(IEvolutionOptions evolutionOptions, IMutator<ulong> mutator, IEventAggregator eventAggregator, int length = 16)
    {
        _evolutionOptions = evolutionOptions;
        _mutator = mutator;
        _eventAggregator = eventAggregator;

        _attractor = Enumerable.Range(0, length).Select(_ => GetRandomStep()).ToArray();
        Dye(length);
        DyeCommand = new DelegateCommand(_ => Dye(Length));
        ToggleEvolutionCommand = new DelegateCommand(_ => IsEvolutionActive = !IsEvolutionActive);
        TogglePitchbendCommand = new DelegateCommand(_ =>
        {
            SendPitchbend = !SendPitchbend;
            if (!SendPitchbend && _lastNote.HasValue)
            {
                MidiDevices.Output.SendEvent(new PitchBendEvent(8192)
                {
                    Channel = (FourBitNumber)_channel
                });
            }
        });
        ToggleViewCommand = new DelegateCommand(_ => ShowSteps = !ShowSteps);
        ToggleRecordCommand = new DelegateCommand(_ => IsRecording = !IsRecording);
        MidiDevices.Input.EventReceived += OnMidiEvent;
        // ±0,5 Halbton: semitones = 0, fraction = 64 (≈ 50 Cent)
        SetPitchBendRange(MidiDevices.Output, _channel, 0, 64);

        _eventAggregator.GetEvent<GlobalKeyEvent>().Subscribe(OnGLobalKeyEvent);
    }

    private bool _leftShiftDown;

    private void OnGLobalKeyEvent(GlobalKeyPayload payload)
    {
        if ((payload.Key == Key.LeftShift || payload.Key == Key.RightShift))
        {
            _leftShiftDown = payload.IsDown;
        }

        if (_isRecording && payload.Key == Key.P && payload.IsDown == false)
        {
            for (var i = 0; i < _steps[_recordingStep].Length; i++)
            {
                var s = new Step(_steps[_recordingStep][i]);
                s.On = false;
                s.Tie = false;
                s.Pitch = 0;
                s.Velocity = 0;
                s.Pitchbend = PitchbendHelpers.RawCenter;
                _steps[_recordingStep][i] = s.Encode();
            }

            RaisePropertyChanged(nameof(Steps));
            SetNotes();
            RecordingStep = (_recordingStep + 1) % _steps.Length;
        }

        if (_isRecording && payload.Key == Key.Left && payload.IsDown == false && _isRecording)
        {
            RecordingStep = (Math.Max(0, _recordingStep - 1)) % _steps.Length;
        }

        if (_isRecording && payload.Key == Key.Right && payload.IsDown == false && _isRecording)
        {
            RecordingStep = (_recordingStep + 1) % _steps.Length;
        }

        if (_isRecording && payload.Key == Key.Home && payload.IsDown == false && _isRecording)
        {
            RecordingStep = 0;
        }

        if (_isRecording && payload.Key == Key.End && payload.IsDown == false && _isRecording)
        {
            RecordingStep = _steps.Length - 1;
        }
    }

    void SetPitchBendRange(OutputDevice dev, int channel, int semitones, int fraction)
    {
        // speichere Range (fraction ist 0..127, dabei ~ fraction/128 Semitone)
        _pitchbendRangeSemitones = semitones + (fraction / 128.0);

        dev.SendEvent(new ControlChangeEvent((SevenBitNumber)101, (SevenBitNumber)0) { Channel = (FourBitNumber)channel }); // RPN MSB
        dev.SendEvent(new ControlChangeEvent((SevenBitNumber)100, (SevenBitNumber)0) { Channel = (FourBitNumber)channel }); // RPN LSB
        dev.SendEvent(new ControlChangeEvent((SevenBitNumber)6, (SevenBitNumber)semitones) { Channel = (FourBitNumber)channel }); // Data Entry MSB
        dev.SendEvent(new ControlChangeEvent((SevenBitNumber)38, (SevenBitNumber)fraction) { Channel = (FourBitNumber)channel }); // Data Entry LSB
        // RPN deselect
        dev.SendEvent(new ControlChangeEvent((SevenBitNumber)101, (SevenBitNumber)127) { Channel = (FourBitNumber)channel });
        dev.SendEvent(new ControlChangeEvent((SevenBitNumber)100, (SevenBitNumber)127) { Channel = (FourBitNumber)channel });
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
                var step = new Step(Fittest(_steps[_currentStep]));
                if (_sendPitchbend)
                    MidiDevices.Output.SendEvent(new PitchBendEvent(step.Pitchbend));
                MidiDevices.Output.SendEvent(new ControlChangeEvent((SevenBitNumber)_controlNumber,
                    (SevenBitNumber)step.ModWheel));

                // Monophone Note-Off/On-Logik
                if (_lastNote.HasValue)
                {
                    // Falls die aktuelle Note nicht weiter gehalten werden soll (kein Tie oder Pitch-Wechsel)
                    if (!step.On || !step.Tie || step.Pitch != _lastNote.Value)
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
                    // Falls eine neue Note gespielt werden soll (Pitch-Wechsel oder keine Note aktiv)
                    if (!_lastNote.HasValue || step.Pitch != _lastNote.Value)
                    {
                        MidiDevices.Output.SendEvent(new NoteOnEvent((SevenBitNumber)step.Pitch,
                            (SevenBitNumber)step.Velocity)
                        {
                            Channel = (FourBitNumber)_channel
                        });
                        _lastNote = step.Pitch;
                    }
                    // Bei Tie mit gleichem Pitch bleibt die Note aktiv, kein erneutes NoteOn nötig
                }

                if (_isEvolutionActive && _tickCount % (ulong)_evolutionOptions.GenerationLength == 0)
                {
                    MutatePopulations();
                    Tournament();
                    RaisePropertyChanged(nameof(Steps));
                    SetNotes();
                }
                _currentStep = (_currentStep + 1) % _steps.Length;
            }
            _tickCount++;
        }

        if (_isRecording && e.Event is NoteOnEvent noteOn)
        {
            for (int i = 0; i < _steps[_recordingStep].Length; i++)
            {
                var s = new Step(_steps[_recordingStep][i]);
                s.On = true;
                s.Tie = _leftShiftDown;
                s.Pitch = noteOn.NoteNumber;
                s.Velocity = noteOn.Velocity;
                s.Pitchbend = _recordingPitchbend;
                _steps[_recordingStep][i] = s.Encode();
            }
            RecordingStep = (_recordingStep + 1) % _steps.Length;
            SetNotes();
            RaisePropertyChanged(nameof(Steps));
        }

        if (_isRecording && e.Event is PitchBendEvent pitchBend)
        {
            _recordingPitchbend = pitchBend.PitchValue;
        }

        if (_isRecording && e.Event is ControlChangeEvent cc)
        {

        }
    }

    private void MutatePopulations()
    {
        for (var i = 0; i < _steps.Length; i++)
        {
            for (var j = 0; j < _steps[i].Length; j++)
            {
                _steps[i][j] = _mutator.Mutate(_steps[i][j], 1.0 / _steps.Length);
            }
        }
    }

    private void Tournament()
    {
        var rand = RandomProvider.Get(_evolutionOptions.Seed);
        for (var i = 0; i < _steps.Length; i++)
        {
            var participants = _steps[i].TakeRandom(10, rand);
            var fittest = participants.GetFittest(2, CombinedFitness);
            var extinct = Extinct(_steps[i]).ToArray();
            if (extinct.Length > _steps[i].Length / 2)
            {
                extinct = extinct.Take(_steps[i].Length / 2).ToArray();
            }

            var offsprings = _mutator.GenerateOffspring(fittest.First(), fittest.Last(), extinct.Length + 1, _evolutionOptions)
                .ToList();
            for (var j = 0; j < extinct.Length; j++)
            {
                _steps[i][extinct[j]] = _mutator.Mutate(offsprings[j],1.0 / _steps[i].Length);
            }
            var leastFittest = _steps[i].MinBy(CombinedFitness);
            _steps[i][Array.IndexOf(_steps[i], leastFittest)] = _mutator.Mutate( offsprings[^1], 1.0 / _steps[i].Length);
        }
    }
    IEnumerable<int> Extinct(ulong[] population, double threshold = 0.1)
    {
        for (int i = 0; i < population.Length; i++)
        {
            if (Fitness.Any(f => f.Weight > 0.1 && f.Evaluate(population[i]) < threshold))
            {
                yield return i;
            }
        }
    }

    private void GenerateRandomSteps(int count)
    {
        _steps = new ulong[count][];
        for (var i = 0; i < _steps.Length; ++i)
        {
            _steps[i] = CreateRandomPopulation();
        }
        RaisePropertyChanged(nameof(Steps));
        SetNotes();
    }

    private ulong GetRandomStep()
    {
        var rand = RandomProvider.Get(_evolutionOptions.Seed);
        byte[] bytes = new byte[8];
        rand.NextBytes(bytes);
        return BitConverter.ToUInt64(bytes, 0);
    }

    public void SetNotes()
    {
        Application.Current.Dispatcher.Invoke(Notes.Clear);
        int i = 0;
        while (i < _steps.Length)
        {
            var fittest = Fittest(_steps[i]);
            var step = new Step(fittest);

            if (step.On)
            {
                // Start new note
                int length = 1;
                var pitch = step.Pitch;
                var velocity = step.Velocity;
                var pitchbend = step.Pitchbend;
                var modWheel = step.ModWheel;

                // Find ties
                int j = i + 1;
                while (j < _steps.Length)
                {
                    var nextFittest = Fittest(_steps[j]);
                    var nextStep = new Step(nextFittest);
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

                // korrekte Umrechnung in Cents (signed, negativ/positiv)
                double cents = PitchbendHelpers.RawToCents(pitchbend, _pitchbendRangeSemitones);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Notes.Add(new ExtendedNote(
                        false,
                        pitch,
                        velocity,
                        length,
                        cents,
                        modWheel
                    ));
                });

                i += length;
            }
            else
            {
                // Count pauses
                int pauseLength = 1;
                int j = i + 1;
                while (j < _steps.Length)
                {
                    var nextFittest = Fittest(_steps[j]);
                    var nextStep = new Step(nextFittest);
                    if (!nextStep.On)
                    {
                        pauseLength++;
                        j++;
                    }
                    else
                    {
                        break;
                    }
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Notes.Add(new ExtendedNote(true, 0, 0, pauseLength, 0, 0));
                });
                i += pauseLength;
            }
        }
    }

    private void SetAttractorNotes()
    {
        Application.Current.Dispatcher.Invoke(AttractorNotes.Clear);
        int i = 0;
        while (i < _attractor.Length)
        {
            var step = new Step(_attractor[i]);

            if (step.On)
            {
                // Start new note
                int length = 1;
                var pitch = step.Pitch;
                var velocity = step.Velocity;
                var pitchbend = step.Pitchbend;
                var modWheel = step.ModWheel;

                // Find ties
                int j = i + 1;
                while (j < _steps.Length)
                {
                    var nextStep = new Step(_attractor[j]);
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

                // korrekte Umrechnung in Cents (signed, negativ/positiv)
                double cents = PitchbendHelpers.RawToCents(pitchbend, _pitchbendRangeSemitones);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    AttractorNotes.Add(new ExtendedNote(
                        false,
                        pitch,
                        velocity,
                        length,
                        cents,
                        modWheel
                    ));
                });

                i += length;
            }
            else
            {
                // Count pauses
                int pauseLength = 1;
                int j = i + 1;
                while (j < _steps.Length)
                {
                    var nextStep = new Step(_attractor[j]);
                    if (!nextStep.On)
                    {
                        pauseLength++;
                        j++;
                    }
                    else
                    {
                        break;
                    }
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    AttractorNotes.Add(new ExtendedNote(true, 0, 0, pauseLength, 0, 0));
                });
                i += pauseLength;
            }
        }
    }

    private ulong[] CreateRandomPopulation()
    {
        return Enumerable.Range(0, _evolutionOptions.PopulationSize)
            .Select(_ => GetRandomStep())
            .ToArray();
    }

    private void Dye(int length)
    {
        GenerateRandomSteps(length);
    }

    double CombinedFitness(ulong individual)
    {
        var total = Fitness.Sum(f => f.Weight);
        return Fitness.Sum(f => f.Evaluate(individual)) / total;
    }

    private ulong Fittest(ulong[] steps)
    {
        var fittest = steps.MaxBy(CombinedFitness);
        return fittest;
    }
}