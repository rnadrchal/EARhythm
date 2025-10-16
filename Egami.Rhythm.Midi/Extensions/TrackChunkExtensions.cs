using Egami.Rhythm.Pattern;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;

namespace Egami.Rhythm.Midi.Extensions;

/// <summary>
/// Ergebnis pro Kanal: Rhythmus, Drum-Score und Programm-Infos.
/// </summary>
public sealed class ChannelPatternSummary
{
    public int Channel { get; init; }                 // 0..15
    public RhythmPattern Pattern { get; init; } = new(0);
    public double DrumProbability { get; init; }

    /// <summary>Distinct-ProgramNumbers (0..127) aller ProgramChange-Events dieses Kanals.</summary>
    public byte[] DistinctPrograms { get; init; } = Array.Empty<byte>();

    /// <summary>Programmnummer, die am/ vor dem ersten NoteOn dieses Kanals aktiv war (falls bekannt).</summary>
    public byte? InitialProgram { get; init; }

    /// <summary>Komplette ProgramChange-Timeline dieses Kanals (Time in Ticks der Datei).</summary>
    public (long TimeTicks, byte Program)[] ProgramTimeline { get; init; } = Array.Empty<(long, byte)>();
}

public static class TrackChunkExtensions
{

    /// <summary>
    /// Extrahiert pro belegtem MIDI-Kanal ein monophones RhythmPattern.
    /// Das Pattern beginnt am ersten Onset des Kanals.
    /// Optional können zu wenige Onsets zyklisch wiederholt werden, um totalSteps zu füllen.
    /// </summary>
    /// <summary>
    /// Extrahiert pro belegtem MIDI-Kanal (nur Kanäle mit Notes) ein monophones RhythmPattern.
    /// Start pro Kanal = erstes NoteOn. Optional können zu wenige Onsets zyklisch wiederholt werden.
    /// Zusätzlich werden Programmnummern (ProgramChange) pro Kanal ermittelt.
    /// </summary>
    public static Dictionary<int, ChannelPatternSummary> ExtractAllChannelPatternsWithProgramsStartingAtFirstNote(
        this TrackChunk chunk,
        TempoMap tempoMap,
        ITimeSpan step,
        int totalSteps,
        bool useNearestQuantization = true,
        bool repeatEventsToFillSteps = false)
    {
        if (chunk is null) throw new ArgumentNullException(nameof(chunk));
        if (tempoMap is null) throw new ArgumentNullException(nameof(tempoMap));
        if (step is null) throw new ArgumentNullException(nameof(step));
        if (totalSteps <= 0) throw new ArgumentOutOfRangeException(nameof(totalSteps));

        var stepTicks = TimeConverter.ConvertFrom(step, tempoMap);
        if (stepTicks <= 0) throw new ArgumentException("step must convert to > 0 ticks.", nameof(step));

        // Notes pro Kanal (nur Kanäle mit mindestens einer Note)
        var allNotes = chunk.GetNotes().ToList();
        var perChannelNotes = allNotes
            .GroupBy(n => (int)n.Channel)
            .ToDictionary(g => g.Key, g => g.OrderBy(n => n.Time).ToList());

        // ProgramChange-Events pro Kanal vorbereiten
        var programChangesByChannel = chunk.Events
            .OfType<ProgramChangeEvent>()
            .GroupBy(p => (int)p.Channel)
            .ToDictionary(
                g => g.Key,
                g => g.Select(p => (TimeTicks: (long)p.DeltaTime /* will be absolute via GetTimedEvents below? */ , Program: (byte)p.ProgramNumber)).ToList()
            );

        // Wichtig: DeltaTime vs. absolute Zeit.
        // Einfacher: hole absolute Zeiten via TimedEvents:
        var timedProgramsByChannel = chunk.GetTimedEvents()
            .Where(te => te.Event is ProgramChangeEvent)
            .GroupBy(te => (int)((ProgramChangeEvent)te.Event).Channel)
            .ToDictionary(
                g => g.Key,
                g => g.Select(te => (TimeTicks: (long)te.Time, Program: (byte)((ProgramChangeEvent)te.Event).ProgramNumber))
                      .OrderBy(t => t.TimeTicks)
                      .ToList()
            );

        var result = new Dictionary<int, ChannelPatternSummary>();

        foreach (var (channel, notesOnChannel) in perChannelNotes.OrderBy(k => k.Key))
        {
            if (notesOnChannel.Count == 0)
                continue;

            long channelStartTicks = (long)notesOnChannel[0].Time;
            long channelEndTicks = channelStartTicks + stepTicks * totalSteps;

            // --- RhythmPattern extrahieren (wie zuvor) ---
            var basePattern = new RhythmPattern(totalSteps)
            {
                Hits = new bool[totalSteps],
                Velocities = new byte[totalSteps],
                Lengths = new int[totalSteps],
                Pitches = new int?[totalSteps],
            };

            var onsets = new List<(int idx, byte vel, int len, byte pitch)>();

            foreach (var n in notesOnChannel)
            {
                long noteStart = (long)n.Time;
                long noteLength = (long)n.Length;

                if (noteStart >= channelEndTicks) continue;
                if (noteStart + noteLength <= channelStartTicks) continue;

                double stepPos = (noteStart - channelStartTicks) / (double)stepTicks;
                int idx = useNearestQuantization
                    ? (int)Math.Round(stepPos, MidpointRounding.AwayFromZero)
                    : (int)Math.Floor(stepPos);

                if (idx < 0 || idx >= totalSteps)
                    continue;

                int lengthSteps = (int)Math.Ceiling(noteLength / (double)stepTicks);
                if (lengthSteps < 1) lengthSteps = 1;
                if (idx + lengthSteps > totalSteps)
                    lengthSteps = totalSteps - idx;

                byte pitch = (byte)n.NoteNumber;
                byte vel = n.Velocity;

                if (!basePattern.Hits[idx] || (basePattern.Pitches[idx] is int p && pitch > p))
                {
                    basePattern.Hits[idx] = true;
                    basePattern.Pitches[idx] = pitch;
                    basePattern.Velocities[idx] = vel;
                    basePattern.Lengths[idx] = lengthSteps;
                }
            }

            for (int i = 0; i < totalSteps; i++)
            {
                if (basePattern.Hits[i])
                    onsets.Add((i, basePattern.Velocities[i], Math.Max(1, basePattern.Lengths[i]), (byte)(basePattern.Pitches[i] ?? 0)));
            }

            RhythmPattern finalPattern;
            if (repeatEventsToFillSteps && onsets.Count > 0 && onsets.Count < totalSteps)
            {
                finalPattern = new RhythmPattern(totalSteps)
                {
                    Hits = Enumerable.Repeat(true, totalSteps).ToArray(),
                    Velocities = new byte[totalSteps],
                    Lengths = new int[totalSteps],
                    Pitches = new int?[totalSteps]
                };

                for (int i = 0; i < totalSteps; i++)
                {
                    var src = onsets[i % onsets.Count];
                    finalPattern.Velocities[i] = src.vel;
                    finalPattern.Lengths[i] = src.len;
                    finalPattern.Pitches[i] = src.pitch;
                }
            }
            else
            {
                finalPattern = basePattern;
            }

            // --- Drum-Score wie gehabt ---
            double drumProb = EvaluateDrumLikelihoodForChannel(chunk, tempoMap, channel, notesOnChannel);

            // --- Program-Infos pro Kanal ---
            timedProgramsByChannel.TryGetValue(channel, out var timedPrograms);
            timedPrograms ??= new List<(long TimeTicks, byte Program)>();

            var distinctPrograms = timedPrograms
                .Select(t => t.Program)
                .Distinct()
                .OrderBy(p => p)
                .ToArray();

            // InitialProgram = letzte Programnummer am/ vor erstem NoteOn
            byte? initialProgram = timedPrograms
                .Where(t => t.TimeTicks <= channelStartTicks)
                .Select(t => (byte?)t.Program)
                .LastOrDefault();

            var summary = new ChannelPatternSummary
            {
                Channel = channel,
                Pattern = finalPattern,
                DrumProbability = drumProb,
                DistinctPrograms = distinctPrograms,
                InitialProgram = initialProgram,
                ProgramTimeline = timedPrograms.ToArray()
            };

            result[channel] = summary;
        }

        return result;
    }         /// Heuristischer Drum-Score [0..1] nur aus Events/Noten des angegebenen Kanals.
              /// Kriterien wie zuvor: Ch.10, GM-Drum-Range, kurze Noten, wenige PitchBends/Sustain.
              /// </summary>
    private static double EvaluateDrumLikelihoodForChannel(
        TrackChunk chunk,
        TempoMap tempoMap,
        int channel,
        List<Note> notesOnChannel)
    {
        int totalNotes = notesOnChannel.Count;
        if (totalNotes == 0) return 0.0;

        // Ch.10 (0-basiert 9)
        double sCh10 = channel == 9 ? 1.0 : 0.0;

        // GM-Drum-Pitches (35..81)
        int gmRange = notesOnChannel.Count(n => n.NoteNumber >= 35 && n.NoteNumber <= 81);
        double sGM = (double)gmRange / totalNotes;

        // Kurze Noten (<= 1/8)
        long eighthTicks = TimeConverter.ConvertFrom(new MusicalTimeSpan(1, 8), tempoMap);
        int shortNotes = notesOnChannel.Count(n => n.Length <= eighthTicks);
        double sShort = (double)shortNotes / totalNotes;

        // Kanalgebundene Events (PitchBend, Sustain CC64) nur für diesen Kanal
        var pitchBends = chunk.Events.OfType<PitchBendEvent>()
            .Count(e => e.Channel == (FourBitNumber)channel);

        var sustainAny = chunk.Events.OfType<ControlChangeEvent>()
            .Count(e => e.Channel == (FourBitNumber)channel &&
                        e.ControlNumber == (SevenBitNumber)64 &&
                        e.ControlValue > 0);

        double sNoPB = 1.0 / (1.0 + pitchBends);
        double sNoSus = 1.0 / (1.0 + sustainAny);

        const double wCh10 = 0.35;
        const double wGM = 0.25;
        const double wShort = 0.20;
        const double wNoPB = 0.10;
        const double wNoSus = 0.10;

        var score =
            wCh10 * sCh10 +
            wGM * sGM +
            wShort * sShort +
            wNoPB * sNoPB +
            wNoSus * sNoSus;

        return Math.Max(0.0, Math.Min(1.0, score));
    }
}