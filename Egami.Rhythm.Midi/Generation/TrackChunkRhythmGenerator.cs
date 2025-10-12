using System.Collections.ObjectModel;
using Egami.Rhythm.Generation;
using Egami.Rhythm.Midi.Extensions;
using Egami.Rhythm.Pattern;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;

namespace Egami.Rhythm.Midi.Generation;

public class TrackChunkRhythmGenerator : IRhythmGenerator
{
    public List<TrackRhythmPattern> TrackPatterns { get; } = new();

    private MidiFile _midi;
    private TempoMap _tempoMap;

    public int Rate { get; set; } = 16;

    public int Index { get; set; }

    public RhythmPattern Generate(RhythmContext ctx)
    {
        ReloadTrackChunks(ctx);
        return TrackPatterns[Index].RhythmPattern;
    }

    public void Load(string filePath, RhythmContext ctx)
    {
        _midi = MidiFile.Read(filePath);
        ReloadTrackChunks(ctx);
    }

    public void ReloadTrackChunks(RhythmContext ctx)
    {
        _tempoMap = _midi.GetTempoMap();
        var track = _midi.GetTrackChunks().First();
        var patternStep = new MusicalTimeSpan(1, Rate);
        TrackPatterns.Clear();

        var perChannel = track.ExtractAllChannelPatternsWithProgramsStartingAtFirstNote(
            _tempoMap, patternStep, ctx.StepsTotal,
            useNearestQuantization: true,
            repeatEventsToFillSteps: true);

        int number = 1;
        foreach (var kv in perChannel)
        {
            var ch = kv.Key;
            var s = kv.Value;
            TrackPatterns.Add(new TrackRhythmPattern(number, s.Pattern, s.DrumProbability, kv.Key, s.DistinctPrograms));
        }
    }
}