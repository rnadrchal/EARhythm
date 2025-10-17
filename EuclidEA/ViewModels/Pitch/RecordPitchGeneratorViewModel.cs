using System;
using System.Windows.Input;
using Egami.Pitch;
using Egami.Rhythm.Midi;
using Egami.Rhythm.Midi.Generation;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using Prism.Commands;

namespace EuclidEA.ViewModels.Pitch;

public class RecordPitchGeneratorViewModel : PitchGeneratorViewModel
{
    private RecordPitchGenerator _generator;
    private int _activeNoteCount;

    private bool _isRecording;

    public bool IsRecording
    {
        get => _isRecording;
        set => SetProperty(ref _isRecording, value);
    }

    private bool _isNoteOn;

    public bool IsNoteOn
    {
        get => _isNoteOn;
        set => SetProperty(ref _isNoteOn, value);
    }

    public string NoteCount => $"{_generator.Pitches.Count:00}";

    public ICommand ToggleStartStopCommand { get; }
    public ICommand ClearCommand { get; }

    public RecordPitchGeneratorViewModel(IPitchGenerator generator) : base(generator)
    {
        _generator = generator as RecordPitchGenerator ?? throw new ArgumentException(nameof(generator));
        ToggleStartStopCommand = new DelegateCommand(ToggleStartStop);
        ClearCommand = new DelegateCommand(OnClear);

        MidiDevices.Input.EventReceived += OnEventReceived;
    }

    private void OnClear()
    {
        _generator.Clear();
        RaisePropertyChanged(nameof(NoteCount));
    }

    public override byte?[] Generate(int length)
    {
        var result = base.Generate(length);
        if (IsRecording)
        {
            _generator.StopRecording();
            IsRecording = false;
        }
        return result;
    }


    private void OnEventReceived(object? sender, MidiEventReceivedEventArgs e)
    {
        if (e.Event is NoteOnEvent noteOn)
        {
            _activeNoteCount++;
            IsNoteOn = true;
            RaisePropertyChanged(nameof(NoteCount));
        }
        if (e.Event is NoteOffEvent noteOff)
        {
            _activeNoteCount--;
            if (_activeNoteCount < 0) _activeNoteCount = 0;
            IsNoteOn = false;
            RaisePropertyChanged(nameof(NoteCount));
        }
    }

    private void ToggleStartStop()
    {
        if (_isRecording)
        {
            _generator.StopRecording();
            IsRecording = false;
        }
        else
        {
            IsRecording = true;
            _generator.StartRecording();
        }
        RaisePropertyChanged(nameof(NoteCount));
    }

    public override string Name => "Learn";
}