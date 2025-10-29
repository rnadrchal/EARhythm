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
using Prism.Mvvm;
using StepMutator.Common;
using StepMutator.Models.Evolution;
using StepMutator.Services;
using Syncfusion.Windows.Shared;
using RandomProvider = Egami.Rhythm.Common.RandomProvider;

namespace StepMutator.Models;

public class Sequence : BindableBase, ISequence
{
    IEvolutionOptions _evolutionOptions;
    private IMutator<ulong> _mutator;
    private ulong[][] _steps;

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
        new PitchbendFitness()
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

    public ObservableCollection<ExtendedNote> Notes { get; } = new();

    public ICommand DyeCommand { get; }
    public ICommand ToggleEvolutionCommand { get; }
    public ICommand TogglePitchbendCommand { get; }
    public ICommand ToggleViewCommand { get; }

    public Sequence(IEvolutionOptions evolutionOptions, IMutator<ulong> mutator, int length = 16)
    {
        _evolutionOptions = evolutionOptions;
        _mutator = mutator;
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
        MidiDevices.Input.EventReceived += OnMidiEvent;
        // ±0,5 Halbton: semitones = 0, fraction = 64 (≈ 50 Cent)
        SetPitchBendRange(MidiDevices.Output, _channel, 0, 64);
    }

    void SetPitchBendRange(OutputDevice dev, int channel, int semitones, int fraction)
    {
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

    public int Length
    {
        get => _steps.Length;
        set
        {
            if (value > _steps.Length)
            {
                var steps = _steps.ToList();
                for (var i = 0; i < value - _steps.Length; ++i)
                {
                    steps.Add(CreateRandomPopulation());
                }
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
                Application.Current.Dispatcher.Invoke(SetNotes);
            }
            RaisePropertyChanged(nameof(Length));
        }
    }
    public IEnumerable<IStep> Steps => _steps.Select(s =>
    {
        var fittest = Fittest(s);
        return new Step(fittest) { Fitness = CombinedFitness(fittest) };
    });

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

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Notes.Add(new ExtendedNote(
                        false,
                        pitch,
                        velocity,
                        length,
                        (pitchbend - 2048) / 2048 * 50,
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