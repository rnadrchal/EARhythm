using System.Collections.ObjectModel;
using Egami.Pitch;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;

namespace Egami.Rhythm.Midi.Generation;

public sealed class RecordPitchGenerator : IPitchGenerator
{
    public ObservableCollection<byte> Pitches { get; private set; } = new();

    public byte?[] Generate(byte basePitch, int length)
    {
        var pitches = Pitches.Count > length
            ? Pitches.Skip(Pitches.Count - length).Take(length)
            : Pitches.Count > 0
                ? Pitches.Concat(Enumerable.Repeat<byte>(basePitch, length - Pitches.Count))
                : Enumerable.Repeat(basePitch, length);
        var result = pitches.Select(b => (byte?)b).ToArray();
        Pitches.Clear();
        return result;
    }

    public void Clear()
    {
        Pitches.Clear();
    }

    public void StartRecording()
    {
        Pitches.Clear();
        MidiDevices.Input.EventReceived += OnEventReceived;
    }

    public void StopRecording()
    {
        MidiDevices.Input.EventReceived -= OnEventReceived;
    }

    private long _currentTick = 0;
    private readonly Dictionary<byte, long> _activeNotes = new();

    private void OnEventReceived(object? sender, MidiEventReceivedEventArgs e)
    {
        _currentTick += e.Event.DeltaTime;

        if (e.Event is NoteOnEvent noteOn && noteOn.Velocity > 0)
        {
            Pitches.Add(noteOn.NoteNumber);
        }
    }
}