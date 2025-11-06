using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Policy;
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
    private readonly FitnessSettings _fitnessSettings;
    private ulong[][] _steps;
    private ulong[] _attractor;

    private ulong[][] _originalSteps = new ulong[][] {};
    private ulong[] _originalAttractor = [];

    private ulong _tickCount = 0;
    private int _currentStep = 0;
    private byte? _lastNote = null;

    private byte _channel;

    public IEvolutionOptions Options => _evolutionOptions;
    public FitnessSettings FitnessSettings => _fitnessSettings;

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

    private bool _isSourceRecording = false;
    public bool IsSourceRecording
    {
        get => _isSourceRecording;
        set
        {
            if (SetProperty(ref _isSourceRecording, value))
            {
                RaisePropertyChanged(nameof(IsRecording));
            }
        }
    }

    private bool _isTargetRecording = false;

    public bool IsTargetRecording
    {
        get => _isTargetRecording;
        set
        {
            if (SetProperty(ref _isTargetRecording, value))
            {
                RaisePropertyChanged(nameof(IsRecording));
            }
        }
    }

    public bool IsRecording => IsSourceRecording || IsTargetRecording;


    private int _recordingStep = 0;
    public int RecordingStep
    {
        get => _recordingStep;
        set => SetProperty(ref _recordingStep, value);
    }

    private byte _recordingControlValue = 0;

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

    public IEnumerable<IStep> Steps => Enumerable.Range(0, _steps.Length).Select(i =>  new Step(Fittest(_steps[i], i)));
    public ObservableCollection<ExtendedNote> AttractorNotes { get; } = new();

    public IEnumerable<IStep> Attractor => _attractor.Select(a => new Step(a));

    public double[] Fitness => Enumerable.Range(0, _steps.Length)
        .Select(i => CombinedFitness(Fittest(_steps[i], i), i))
        .ToArray();

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
            RaisePropertyChanged(nameof(Fitness));
        }
    }

    public ICommand DyeCommand { get; }
    public ICommand DyeAttractorCommand { get; }
    public ICommand ToggleEvolutionCommand { get; }
    public ICommand TogglePitchbendCommand { get; }
    public ICommand ToggleViewCommand { get; }
    public ICommand ToggleSourceRecordingCommand { get; }
    public ICommand ToggleTargetRecordingCommand { get; }
    public ICommand RevertCommand { get; }

    public Sequence(IEvolutionOptions evolutionOptions, IMutator<ulong> mutator, IEventAggregator eventAggregator, FitnessSettings fitnessSettings, int length = 16)
    {
        _evolutionOptions = evolutionOptions;
        _mutator = mutator;
        _eventAggregator = eventAggregator;
        _fitnessSettings = fitnessSettings;

        _attractor = Enumerable.Range(0, length).Select(_ => GetRandomStep()).ToArray();
        SetAttractorNotes();
        Dye(length);
        DyeCommand = new DelegateCommand(_ => Dye(Length));
        DyeAttractorCommand = new DelegateCommand(_ => DyeAttractor());
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
        ToggleSourceRecordingCommand = new DelegateCommand(_ => ToggleSourceRecording());
        ToggleTargetRecordingCommand = new DelegateCommand(_ => ToggleTargetRecording());
        RevertCommand = new DelegateCommand(_ => RevertStepsAndAttractor());
        MidiDevices.Input.EventReceived += OnMidiEvent;
        // ±0,5 Halbton: semitones = 0, fraction = 64 (≈ 50 Cent)
        SetPitchBendRange(MidiDevices.Output, _channel, 0, 64);

        _eventAggregator.GetEvent<GlobalKeyEvent>().Subscribe(OnGLobalKeyEvent);
    }

    private void ToggleSourceRecording()
    {
        IsSourceRecording = !IsSourceRecording;
        if (!IsRecording)
        {
            CloneStepsAndAttractor();
        }
    }

    private void ToggleTargetRecording()
    {
        IsTargetRecording = !IsTargetRecording;
        if (!IsRecording)
        {
            CloneStepsAndAttractor();
        }
    }

    private void CloneStepsAndAttractor()
    {
        // Deep copy: jede Population (ulong[]) klonen, so dass _originalSteps unabhängig ist
        _originalSteps = _steps?.Select(pop => pop?.ToArray() ?? Array.Empty<ulong>()).ToArray() ?? Array.Empty<ulong[]>();

        // Deep copy des Attractors
        _originalAttractor = _attractor?.ToArray() ?? Array.Empty<ulong>();
    }

    private void RevertStepsAndAttractor()
    {
        _steps = _originalSteps?.Select(pop => pop?.ToArray() ?? Array.Empty<ulong>()).ToArray() ?? Array.Empty<ulong[]>();
        _attractor = _originalAttractor?.ToArray() ?? Array.Empty<ulong>();
        SetNotes();
        SetAttractorNotes();
        RaisePropertyChanged(nameof(Length));
        RaisePropertyChanged(nameof(Steps));
        RaisePropertyChanged(nameof(Attractor));
    }

    private bool _leftShiftDown;

    private void OnGLobalKeyEvent(GlobalKeyPayload payload)
    {
        if ((payload.Key == Key.LeftShift || payload.Key == Key.RightShift))
        {
            _leftShiftDown = payload.IsDown;
        }

        if (IsRecording && payload.Key == Key.P && payload.IsDown == false)
        {
            if (IsSourceRecording)
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
            }

            if (IsTargetRecording)
            {
                var s = new Step(_attractor[_recordingStep])
                {
                    On = false,
                    Tie = false,
                    Pitch = 0,
                    Velocity = 0,
                    Pitchbend = PitchbendHelpers.RawCenter
                };
                _attractor[_recordingStep] = s.Encode();

                RaisePropertyChanged(nameof(Attractor));
                SetAttractorNotes();
            }

            RecordingStep = (_recordingStep + 1) % _steps.Length;
        }

        if (IsRecording && payload is { Key: Key.Left, IsDown: false })
        {
            RecordingStep = (Math.Max(0, _recordingStep - 1)) % _steps.Length;
        }

        if (IsRecording && payload is { Key: Key.Right, IsDown: false })
        {
            RecordingStep = (_recordingStep + 1) % _steps.Length;
        }

        if (IsRecording && payload is { Key: Key.Home, IsDown: false })
        {
            RecordingStep = 0;
        }

        if (IsRecording && payload is { Key: Key.End, IsDown: false })
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
                var step = new Step(Fittest(_steps[_currentStep], _currentStep));
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
                    RaisePropertyChanged(nameof(Fitness));
                    SetNotes();
                }
                _currentStep = (_currentStep + 1) % _steps.Length;
            }
            _tickCount++;
        }

        if (IsRecording && e.Event is NoteOnEvent noteOn)
        {
            if (IsSourceRecording)
            {
                for (int i = 0; i < _steps[_recordingStep].Length; i++)
                {
                    var s = new Step(_steps[_recordingStep][i]);
                    s.On = true;
                    s.Tie = _leftShiftDown;
                    s.Pitch = noteOn.NoteNumber;
                    s.Velocity = noteOn.Velocity;
                    s.Pitchbend = _recordingPitchbend;
                    s.ModWheel = _recordingControlValue;
                    _steps[_recordingStep][i] = s.Encode();
                }
                SetNotes();
                RaisePropertyChanged(nameof(Steps));
            }

            if (IsTargetRecording)
            {
                var s = new Step(_attractor[_recordingStep]);
                s.On = true;
                s.Tie = _leftShiftDown;
                s.Pitch = noteOn.NoteNumber;
                s.Velocity = noteOn.Velocity;
                s.Pitchbend = _recordingPitchbend;
                s.ModWheel = _recordingControlValue;
                _attractor[_recordingStep] = s.Encode();
                SetAttractorNotes();
                RaisePropertyChanged(nameof(Attractor));
            }
            RaisePropertyChanged(nameof(Fitness));
            RecordingStep = (_recordingStep + 1) % _steps.Length;
        }

        if (IsRecording && e.Event is PitchBendEvent pitchBend)
        {
            _recordingPitchbend = pitchBend.PitchValue;
        }

        if (IsRecording && e.Event is ControlChangeEvent cc && cc.ControlNumber == 0)
        {
            _recordingControlValue = cc.ControlValue;
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
            var fittest = _steps[i].GetFittest(_evolutionOptions.TournamentSize, individual => CombinedFitness(individual, i));
            var participants = fittest.TakeRandom(2, rand);
            var extinct = Extinct(i, _evolutionOptions.ExtinctionThreshold).ToArray();
            if (extinct.Length > _steps[i].Length * _evolutionOptions.ExtinctionRate)
            {
                extinct = extinct.Take((int)(_steps[i].Length * _evolutionOptions.ExtinctionRate)).ToArray();
            }

            var replacements = _mutator.GenerateOffspring(participants.First(), participants.Last(), extinct.Length,
                _evolutionOptions)
                .ToList();
            int j;
            for (j = 0; j < extinct.Length; j++)
            {
                _steps[i][extinct[j]] = _mutator.Mutate(replacements[j], 1.0 / _steps[i].Length);
            }

            var offspringCount = rand.Next(1, _evolutionOptions.MaxOffsprings);
            var offsprings = _mutator.GenerateOffspring(participants.First(), participants.Last(), offspringCount, _evolutionOptions)
                .ToList();
            var leastFittest = _steps[i].OrderBy(individual => CombinedFitness(individual, i)).Take(offspringCount);
            j = 0;
            foreach (var individual in leastFittest)
            {
                _steps[i][Array.IndexOf(_steps[i], individual)] = _mutator.Mutate(offsprings[j++], i);
            }

        }
    }
    IEnumerable<int> Extinct(int stepIndex, double threshold = 0.1)
    {
        var target = new Step(_attractor[stepIndex]);
        var population = _steps[stepIndex];
        for (int i = 0; i < population.Length; i++)
        {
            if (_fitnessSettings.WeightHit > 0 && new HitFitness(target.On) { Weight = _fitnessSettings.WeightHit }.Evaluate(population[i]) < threshold) yield return i;
            if (_fitnessSettings.WeightPitch > 0 && new PitchFitness(target.Pitch){Weight = _fitnessSettings.WeightPitch}.Evaluate(population[i]) < threshold) yield return i;
            if (_fitnessSettings.WeightVelocity > 0 && new VelocityFitness(target.Velocity) { Weight = _fitnessSettings.WeightVelocity }.Evaluate(population[i]) < threshold) yield return i;
            if (_fitnessSettings.WeightTie > 0 && new TieFitness(target.Tie) { Weight = _fitnessSettings.WeightTie }.Evaluate(population[i]) <= threshold) yield return i;
            if (_fitnessSettings.WeightPitchbend > 0 && new PitchbendFitness(target.Pitchbend) { Weight = _fitnessSettings.WeightPitchbend }.Evaluate(population[i]) < threshold) yield return i;
            if (_fitnessSettings.WeightModulation > 0 && new ModulationFitness(target.ModWheel) { Weight = _fitnessSettings.WeightModulation }.Evaluate(population[i]) < threshold) yield return i;
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
            var fittest = Fittest(_steps[i], i);
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
                    var nextFittest = Fittest(_steps[j], j);
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
                    var nextFittest = Fittest(_steps[j], j);
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
                while (j < _attractor.Length)
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
                while (j < _attractor.Length)
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
        CloneStepsAndAttractor();
        SetNotes();
        RaisePropertyChanged(nameof(Steps));
        RaisePropertyChanged(nameof(Fitness));
    }

    private void DyeAttractor()
    {
        _attractor = Enumerable.Range(0, _attractor.Length).Select(_ => GetRandomStep()).ToArray();
        CloneStepsAndAttractor();
        SetAttractorNotes();
        RaisePropertyChanged(nameof(Attractor));
        RaisePropertyChanged(nameof(Fitness));
    }

    double CombinedFitness(ulong individual, int stepIndex)
    {
        var total = _fitnessSettings.WeightHit + _fitnessSettings.WeightTie + _fitnessSettings.WeightPitch +
                    _fitnessSettings.WeightVelocity + _fitnessSettings.WeightPitchbend
                    + _fitnessSettings.WeightModulation;
        var target = new Step(_attractor[stepIndex]);
        var sum = new PitchFitness(target.Pitch) { Weight = _fitnessSettings.WeightPitch}.Evaluate(individual);
        sum += new VelocityFitness(target.Velocity) { Weight = _fitnessSettings.WeightVelocity }.Evaluate(individual);
        sum += new HitFitness(target.On) { Weight = _fitnessSettings.WeightHit }.Evaluate(individual);
        sum += new TieFitness(target.Tie) { Weight = _fitnessSettings.WeightTie }.Evaluate(individual);
        sum += new PitchbendFitness(target.Pitchbend) { Weight = _fitnessSettings.WeightPitchbend }.Evaluate(individual);
        sum += new ModulationFitness(target.ModWheel) { Weight = _fitnessSettings.WeightModulation }.Evaluate(individual);

        return sum / total;
    }

    private ulong Fittest(ulong[] steps, int stepIndex)
    {
        var fittest = steps.MaxBy(individual => CombinedFitness(individual, stepIndex));
        return fittest;
    }
}