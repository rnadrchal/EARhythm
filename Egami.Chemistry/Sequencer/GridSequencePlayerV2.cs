using Egami.Sequencer;
using Egami.Sequencer.Grid;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;

namespace Egami.Chemistry.Sequencer;

public sealed class GridSequencePlayerV2 : IDisposable
{
    private readonly OutputDevice _outputDevice;
    private readonly FourBitNumber _channel;
    private readonly MidiClockGrid _clockGrid;

    private readonly object _sync = new();

    private MusicalSequence _sequence = MusicalSequence.Empty;
    private Dictionary<int, List<SequenceStep>> _stepsByStart = new();

    private readonly List<ActiveNote> _activeNotes = new();
    private readonly List<ActiveRampJob> _activeRampJobs = new();

    private int _currentStepIndex;
    private bool _isPlaying;

    private int _transposeSemitones; // -48..+48
    private bool _legato;

    public GridSequencePlayerV2(OutputDevice outputDevice, FourBitNumber channel, MidiClockGrid clockGrid)
    {
        _outputDevice = outputDevice ?? throw new ArgumentNullException(nameof(outputDevice));
        _channel = channel;
        _clockGrid = clockGrid ?? throw new ArgumentNullException(nameof(clockGrid));

        _clockGrid.GridTick += OnGridTick;
        _clockGrid.Pulse += OnPulse;
    }

    public bool IsPlaying
    {
        get { lock (_sync) return _isPlaying; }
    }

    public int TransposeSemitones
    {
        get { lock (_sync) return _transposeSemitones; }
        set { lock (_sync) _transposeSemitones = Math.Clamp(value, -48, 48); }
    }

    public bool Legato
    {
        get { lock (_sync) return _legato; }
        set { lock (_sync) _legato = value; }
    }

    public void SetSequence(MusicalSequence sequence, bool resetPosition = false)
    {
        if (sequence == null) throw new ArgumentNullException(nameof(sequence));

        lock (_sync)
        {
            _sequence = sequence;

            _stepsByStart = _sequence.Steps
                .GroupBy(s => s.StepIndex)
                .ToDictionary(g => g.Key, g => g.ToList());

            if (resetPosition)
                _currentStepIndex = 0;
        }
    }

    public void Start(bool resetPosition = true)
    {
        lock (_sync)
        {
            if (_isPlaying) return;
            _isPlaying = true;

            if (resetPosition)
                _currentStepIndex = 0;

            // Safety: keine hängenden Noten
            StopAllNotes_NoLock();
            _activeRampJobs.Clear();
        }
    }

    public void Stop()
    {
        lock (_sync)
        {
            if (!_isPlaying) return;
            _isPlaying = false;

            StopAllNotes_NoLock();
            _activeRampJobs.Clear();
        }
    }

    public void Dispose()
    {
        _clockGrid.GridTick -= OnGridTick;
        _clockGrid.Pulse -= OnPulse;
        Stop();
    }

    // -------------------- Clock handlers --------------------

    private void OnPulse(int pulseIndex, int pulsesPerStep)
    {
        lock (_sync)
        {
            if (!_isPlaying) return;
            if (_activeRampJobs.Count == 0) return;

            // Pulse-Advance aller aktiven Ramp-Jobs
            for (int i = _activeRampJobs.Count - 1; i >= 0; i--)
            {
                var job = _activeRampJobs[i];
                if (job.AdvanceAndMaybeSend(_outputDevice))
                {
                    _activeRampJobs.RemoveAt(i);
                }
            }
        }
    }

    private void OnGridTick()
    {
        lock (_sync)
        {
            if (!_isPlaying) return;
            if (_sequence.LengthInSteps == 0) return;

            // 1) ActiveNotes runterzählen und ggf. NoteOff senden
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

            // 2) Steps starten, die auf _currentStepIndex liegen
            if (_stepsByStart.TryGetValue(_currentStepIndex, out var steps))
            {
                foreach (var step in steps)
                {
                    StartStep(step);
                }
            }

            // 3) Nächster Step (Loop)
            _currentStepIndex++;
            if (_currentStepIndex >= _sequence.LengthInSteps)
                _currentStepIndex = 0;
        }
    }

    // -------------------- Step start --------------------

    private void StartStep(SequenceStep step)
    {
        var ext = step as IGridAutomatedStep;

        var ignorePitch = ext?.IgnorePitch ?? false;
        var ignorePb = ext?.IgnorePitchbend ?? false;
        var ignoreCc = ext?.IgnoreCc ?? false;

        var effPitch = GetEffectivePitch(step.Pitch);
        var effPb = ignorePb ? 0 : step.PitchBend;

        // CCs
        if (!ignoreCc)
            SendCcs(step);

        // Pitchbend (initial)
        if (!ignorePb)
            SendPitchBendValue(effPb);

        // Ramps starten (über Pulse)
        if (ext != null)
            ArmRamps(ext);

        // NoteOn / Legato
        if (!ignorePitch)
        {
            if (_legato)
            {
                // Legato nur, wenn Pitch UND Pitchbend identisch
                var existing = _activeNotes.FirstOrDefault(n =>
                    n.EffectivePitch == effPitch &&
                    n.EffectivePitchBend == effPb);

                if (existing != null)
                {
                    existing.RemainingSteps = Math.Max(existing.RemainingSteps, step.LengthInSteps);
                    return; // kein retrigger
                }
            }

            SendNoteOn(effPitch, step.Velocity);
            _activeNotes.Add(new ActiveNote(step.LengthInSteps, effPitch, effPb));
        }
    }

    private void ArmRamps(IGridAutomatedStep step)
    {
        // Pitchbend ramp
        if (!step.IgnorePitchbend && step.PitchbendRamp is { } pb)
        {
            if (pb.DurationPulses > 0)
            {
                // send start immediately (optional, aber stabil)
                SendPitchBendValue(pb.StartValue);

                _activeRampJobs.Add(ActiveRampJob.CreatePitchbend(
                    _channel,
                    pb.StartValue,
                    pb.EndValue,
                    pb.DurationPulses));
            }
        }

        // CC ramps
        if (!step.IgnoreCc && step.CcRamps is { Count: > 0 } ramps)
        {
            foreach (var r in ramps)
            {
                if (r.DurationPulses <= 0) continue;
                if (r.CcNumber is < 0 or > 127) continue;

                // start immediately
                SendCc(r.CcNumber, r.StartValue);

                _activeRampJobs.Add(ActiveRampJob.CreateCc(
                    _channel,
                    r.CcNumber,
                    r.StartValue,
                    r.EndValue,
                    r.DurationPulses));
            }
        }
    }

    // -------------------- MIDI send helpers --------------------

    private int GetEffectivePitch(SevenBitNumber pitch)
    {
        var p = pitch + _transposeSemitones;
        if (p < 0) p = 0;
        if (p > 127) p = 127;
        return p;
    }

    private void SendNoteOn(int effectivePitch, SevenBitNumber velocity)
    {
        var ev = new NoteOnEvent((SevenBitNumber)effectivePitch, velocity)
        {
            Channel = _channel
        };
        _outputDevice.SendEvent(ev);
    }

    private void SendNoteOff(int effectivePitch)
    {
        var ev = new NoteOffEvent((SevenBitNumber)effectivePitch, (SevenBitNumber)0)
        {
            Channel = _channel
        };
        _outputDevice.SendEvent(ev);
    }

    private void SendCcs(SequenceStep step)
    {
        foreach (var kv in step.CcValues)
            SendCc(kv.Key, kv.Value);
    }

    private void SendCc(int ccNumber, SevenBitNumber value)
    {
        if (ccNumber is < 0 or > 127) return;

        var ev = new ControlChangeEvent((SevenBitNumber)ccNumber, value)
        {
            Channel = _channel
        };
        _outputDevice.SendEvent(ev);
    }

    private void SendPitchBendValue(int pitchBend)
    {
        // pitchBend: -8192..+8191 => event 0..16383
        var value = pitchBend + 8192;
        if (value < 0) value = 0;
        if (value > 16383) value = 16383;

        var ev = new PitchBendEvent((ushort)value)
        {
            Channel = _channel
        };
        _outputDevice.SendEvent(ev);
    }

    private void StopAllNotes_NoLock()
    {
        // NoteOff für alle aktiven Noten
        for (int i = _activeNotes.Count - 1; i >= 0; i--)
        {
            SendNoteOff(_activeNotes[i].EffectivePitch);
        }
        _activeNotes.Clear();

        // Optional: "All Notes Off" CC123 (zusätzlich robust)
        var allNotesOff = new ControlChangeEvent((SevenBitNumber)123, (SevenBitNumber)0)
        {
            Channel = _channel
        };
        _outputDevice.SendEvent(allNotesOff);
    }

    // -------------------- internal state --------------------

    private sealed class ActiveNote
    {
        public int RemainingSteps { get; set; }
        public int EffectivePitch { get; }
        public int EffectivePitchBend { get; }

        public ActiveNote(int lengthInSteps, int effectivePitch, int effectivePitchBend)
        {
            RemainingSteps = lengthInSteps;
            EffectivePitch = effectivePitch;
            EffectivePitchBend = effectivePitchBend;
        }
    }

    private sealed class ActiveRampJob
    {
        private readonly FourBitNumber _channel;
        private readonly RampKind _kind;

        private readonly int _ccNumber; // only for CC
        private readonly int _startInt;
        private readonly int _endInt;
        private readonly int _durationPulses;

        private int _elapsedPulses;
        private int _lastSent; // pitchbend int or cc int

        private ActiveRampJob(
            FourBitNumber channel,
            RampKind kind,
            int ccNumber,
            int startInt,
            int endInt,
            int durationPulses)
        {
            _channel = channel;
            _kind = kind;
            _ccNumber = ccNumber;
            _startInt = startInt;
            _endInt = endInt;
            _durationPulses = durationPulses;

            _elapsedPulses = 0;
            _lastSent = startInt;
        }

        public static ActiveRampJob CreatePitchbend(FourBitNumber channel, int start, int end, int durationPulses)
            => new(channel, RampKind.Pitchbend, -1, start, end, durationPulses);

        public static ActiveRampJob CreateCc(FourBitNumber channel, int ccNumber, SevenBitNumber start, SevenBitNumber end, int durationPulses)
            => new(channel, RampKind.Cc, ccNumber, start, end, durationPulses);

        /// <summary>
        /// Returns true if job is finished.
        /// </summary>
        public bool AdvanceAndMaybeSend(OutputDevice device)
        {
            _elapsedPulses++;
            var t = _durationPulses <= 0 ? 1.0 : Math.Min(1.0, (double)_elapsedPulses / _durationPulses);
            var value = (int)Math.Round(_startInt + ((_endInt - _startInt) * t));

            if (value != _lastSent)
            {
                if (_kind == RampKind.Pitchbend)
                {
                    // -8192..+8191
                    var pb = Math.Clamp(value, -8192, 8191);
                    var evValue = pb + 8192;
                    var ev = new PitchBendEvent((ushort)Math.Clamp(evValue, 0, 16383))
                    {
                        Channel = _channel
                    };
                    device.SendEvent(ev);
                }
                else
                {
                    var ccVal = Math.Clamp(value, 0, 127);
                    var ev = new ControlChangeEvent((SevenBitNumber)_ccNumber, (SevenBitNumber)ccVal)
                    {
                        Channel = _channel
                    };
                    device.SendEvent(ev);
                }

                _lastSent = value;
            }

            return _elapsedPulses >= _durationPulses;
        }

        private enum RampKind
        {
            Pitchbend,
            Cc
        }
    }
}
