using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;

namespace Egami.Sequencer.Grid;

public sealed class GridSequencePlayer : IDisposable
{
    private readonly OutputDevice _outputDevice;
    private readonly FourBitNumber _channel;
    private readonly MidiClockGrid _clockGrid;

    private readonly object _sync = new();

    private MusicalSequence _sequence;
    private Dictionary<int, List<SequenceStep>> _stepsByStart = new();
    private readonly List<ActiveNote> _activeNotes = new();

    private int _currentStepIndex;
    private bool _isPlaying;

    private int _transposeSemitones;    // -48..+48
    private bool _legato;

    private sealed class ActiveNote
    {
        public SequenceStep Step { get; }
        public int RemainingSteps { get; set; }

        /// <summary>
        /// Effektiver Pitch am Zeitpunkt des NoteOn (inkl. Transpose).
        /// </summary>
        public int EffectivePitch { get; }

        public ActiveNote(SequenceStep step, int effectivePitch)
        {
            Step = step;
            RemainingSteps = step.LengthInSteps;
            EffectivePitch = effectivePitch;
        }
    }

    public GridSequencePlayer(
        MusicalSequence sequence,
        OutputDevice outputDevice,
        FourBitNumber channel,
        MidiClockGrid clockGrid)
    {
        _sequence = sequence ?? throw new ArgumentNullException(nameof(sequence));
        _outputDevice = outputDevice ?? throw new ArgumentNullException(nameof(outputDevice));
        _channel = channel;
        _clockGrid = clockGrid ?? throw new ArgumentNullException(nameof(clockGrid));

        RebuildIndex();

        _clockGrid.GridTick += OnGridTick;
    }

    /// <summary>
    /// Aktuelle Sequenz (read-only von außen).
    /// </summary>
    public MusicalSequence Sequence
    {
        get
        {
            lock (_sync)
            {
                return _sequence;
            }
        }
    }

    public bool IsPlaying
    {
        get
        {
            lock (_sync)
            {
                return _isPlaying;
            }
        }
    }

    /// <summary>
    /// Transposition in Halbtönen (-48..+48).
    /// </summary>
    public int TransposeSemitones
    {
        get
        {
            lock (_sync) return _transposeSemitones;
        }

        set
        {
            lock (_sync)
            {
                _transposeSemitones = Math.Clamp(value, -48, 48);
            }
        }
    }

    /// <summary>
    /// Legato-Modus:
    /// true = eine bereits klingende Note (gleicher effektiver Pitch) wird nicht erneut angeschlagen,
    /// sondern nur ihre Dauer verlängert.
    /// </summary>
    public bool Legato
    {
        get
        {
            lock (_sync) return _legato;
        }
        set
        {
            lock (_sync) _legato = value;
        }
    }

    /// <summary>
    /// Neue Sequenz setzen. 
    /// resetPosition = true: Ab nächstem GridTick bei Step 0 neu beginnen.
    /// resetPosition = false: Im aktuellen Step weiterlaufen (Loop passt sich an).
    /// </summary>
    public void SetSequence(MusicalSequence sequence, bool resetPosition = false)
    {
        if (sequence == null) throw new ArgumentNullException(nameof(sequence));

        lock (_sync)
        {
            _sequence = sequence;
            RebuildIndex();

            if (resetPosition)
            {
                _currentStepIndex = 0;
                _activeNotes.Clear();
            }
            else
            {
                if (_sequence.LengthInSteps > 0)
                {
                    _currentStepIndex %= _sequence.LengthInSteps;
                }
                else
                {
                    _currentStepIndex = 0;
                    _activeNotes.Clear();
                }
            }
        }
    }

    /// <summary>
    /// Grid-Auflösung während der Wiedergabe ändern.
    /// </summary>
    public void SetGridDivision(GridDivision division, bool resetPhase = false)
    {
        _clockGrid.SetDivision(division, resetPhase);
    }

    public void Start()
    {
        lock (_sync)
        {
            _currentStepIndex = 0;
            _activeNotes.Clear();
            _isPlaying = true;
        }
    }

    public void Stop()
    {
        lock (_sync)
        {
            _isPlaying = false;
            AllNotesOff_NoLock();
        }
    }

    private void RebuildIndex()
    {
        _stepsByStart = _sequence.Steps
            .GroupBy(s => s.StepIndex)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    private void OnGridTick()
    {
        lock (_sync)
        {
            if (!_isPlaying || _sequence.LengthInSteps == 0)
                return;

            // 1. Aktive Noten updaten und ggf. NoteOff senden
            for (int i = _activeNotes.Count - 1; i >= 0; i--)
            {
                var note = _activeNotes[i];
                note.RemainingSteps--;

                if (note.RemainingSteps <= 0)
                {
                    SendNoteOff(note.EffectivePitch);
                    _activeNotes.RemoveAt(i);
                }
            }

            // 2. Neue Steps starten
            if (_stepsByStart.TryGetValue(_currentStepIndex, out var stepsToStart))
            {
                foreach (var step in stepsToStart)
                {
                    // effektiver Pitch (inkl. Transpose) bestimmen
                    int effPitch = ClampPitch(step.Pitch + _transposeSemitones);

                    if (_legato)
                    {
                        // Gibt es bereits eine aktive Note mit gleichem effektiven Pitch?
                        var existing = _activeNotes.FirstOrDefault(n => n.EffectivePitch == effPitch);

                        if (existing != null)
                        {
                            // Keine neue Note, nur Dauer verlängern:
                            existing.RemainingSteps = Math.Max(
                                existing.RemainingSteps,
                                step.LengthInSteps);

                            // Optional: CC1/Pitchbend trotzdem updaten?
                            // Hier: ja, damit Mod/Glide sich trotzdem ändern können.
                            SendCcs(step);
                            SendPitchBend(step);
                            continue;
                        }
                    }

                    // Normalfall (oder Legato, aber noch keine aktive Note mit diesem Pitch):
                    SendCcs(step);
                    SendPitchBend(step);
                    SendNoteOn(effPitch, step);

                    _activeNotes.Add(new ActiveNote(step, effPitch));
                }
            }

            // 3. Zum nächsten Grid-Step (Loop)
            _currentStepIndex++;
            if (_currentStepIndex >= _sequence.LengthInSteps)
                _currentStepIndex = 0;
        }
    }

    private void AllNotesOff_NoLock()
    {
        foreach (var note in _activeNotes)
        {
            SendNoteOff(note.EffectivePitch);
        }

        _activeNotes.Clear();
    }

    private static int ClampPitch(int pitch)
    {
        return Math.Clamp(pitch, 0, 127);
    }

    private void SendNoteOn(int pitch, SequenceStep step)
    {
        var noteOn = new NoteOnEvent((SevenBitNumber)pitch, step.Velocity)
        {
            Channel = _channel
        };

        _outputDevice.SendEvent(noteOn);
    }

    private void SendNoteOff(int pitch)
    {
        var noteOff = new NoteOffEvent((SevenBitNumber)pitch, (SevenBitNumber)0)
        {
            Channel = _channel
        };

        _outputDevice.SendEvent(noteOff);
    }

    private void SendCcs(SequenceStep step)
    {
        foreach (var kv in step.CcValues)
        {
            int ccNumber = kv.Key;
            var ccValue = kv.Value;

            var ev = new ControlChangeEvent((SevenBitNumber)ccNumber, ccValue)
            {
                Channel = _channel
            };

            _outputDevice.SendEvent(ev);
        }
    }

    private void SendPitchBend(SequenceStep step)
    {
        if (step.PitchBend == 0)
            return;

        // PitchBendEvent erwartet 0..16383, 8192 = center
        var value = step.PitchBend + 8192;
        if (value < 0) value = 0;
        if (value > 16383) value = 16383;

        var pitchBend = new PitchBendEvent((ushort)value)
        {
            Channel = _channel
        };

        _outputDevice.SendEvent(pitchBend);
    }

    public void Dispose()
    {
        _clockGrid.GridTick -= OnGridTick;
    }
}