using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Egami.Sequencer.Grid;

public enum LfoWaveform
{
    Sine,
    Saw,
    Triangle,
    Square
}

public enum LfoTargetType
{
    ControlChange,
    PitchBend
}

public sealed class LfoDefinition
{
    public int StepIndex { get; init; }
    public int LengthInSteps { get; init; } = 1;

    public LfoTargetType TargetType { get; init; }

    // for CC target
    public int CcNumber { get; init; }
    public byte CcMin { get; init; } = 0;
    public byte CcMax { get; init; } = 127;

    // for PitchBend target (signed range -8192..+8191)
    public int PitchBendMin { get; init; } = -8192;
    public int PitchBendMax { get; init; } = 8191;

    public LfoWaveform Waveform { get; init; } = LfoWaveform.Sine;

    /// <summary>
    /// Pulse width for square wave (0..1)
    /// </summary>
    public double PulseWidth { get; init; } = 0.5;

    /// <summary>
    /// Phase offset [0..1)
    /// </summary>
    public double Phase { get; init; } = 0.0;

    /// <summary>
    /// Sequence of amplitudes. For ControlChange values are expected0..127. For PitchBend values are signed and represent the target amplitude (distance from center) to oscillate between0 and that value.
    /// The sequence will be looped. Sequence granularity for amplitude selection is per-step (one amplitude value applies for the whole step).
    /// </summary>
    public int[] Amplitudes { get; init; } = Array.Empty<int>();

    /// <summary>
    /// Period length in whole notes. A single waveform cycle spans this many whole notes.
    /// </summary>
    public int PeriodInWholeNotes { get; init; } = 1;

    public bool Enabled { get; init; } = true;
}

public sealed class GridLfoPlayer : IDisposable
{
    private readonly OutputDevice _outputDevice;
    private readonly FourBitNumber _channel;
    private readonly MidiClockGrid _clockGrid;

    private readonly object _sync = new();

    private List<LfoDefinition> _definitions = new();
    private Dictionary<int, List<LfoDefinition>> _defsByStart = new();

    private readonly List<ActiveLfo> _active = new();
    private int _currentStepIndex;
    private bool _isRunning;

    private sealed class ActiveLfo
    {
        public LfoDefinition Def { get; }
        public int RemainingSteps { get; set; }
        public long ElapsedPulses { get; set; }

        // last sent CC value to avoid redundant sends
        public int? LastSentCc { get; set; }

        public ActiveLfo(LfoDefinition def)
        {
            Def = def;
            RemainingSteps = def.LengthInSteps;
            ElapsedPulses = 0;
            LastSentCc = null;
        }
    }

    public GridLfoPlayer(
    OutputDevice outputDevice,
    FourBitNumber channel,
    MidiClockGrid clockGrid)
    {
        _outputDevice = outputDevice ?? throw new ArgumentNullException(nameof(outputDevice));
        _channel = channel;
        _clockGrid = clockGrid ?? throw new ArgumentNullException(nameof(clockGrid));

        _clockGrid.GridTick += OnGridTick;
        _clockGrid.Pulse += OnPulse;
    }

    public void SetDefinitions(IEnumerable<LfoDefinition> definitions)
    {
        if (definitions == null) throw new ArgumentNullException(nameof(definitions));
        lock (_sync)
        {
            _definitions = definitions.Where(d => d is { Enabled: true }).ToList();
            RebuildIndex();
        }
    }

    private void RebuildIndex()
    {
        _defsByStart = _definitions
        .GroupBy(d => d.StepIndex)
        .ToDictionary(g => g.Key, g => g.ToList());
    }

    public void Start()
    {
        lock (_sync)
        {
            _currentStepIndex = 0;
            _active.Clear();
            _isRunning = true;
        }
    }

    public void Stop()
    {
        lock (_sync)
        {
            _isRunning = false;
            _active.Clear();
        }
    }

    private void OnGridTick()
    {
        lock (_sync)
        {
            if (!_isRunning)
                return;

            // decrement remaining steps and remove finished
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var a = _active[i];
                a.RemainingSteps--;
                if (a.RemainingSteps <= 0)
                {
                    _active.RemoveAt(i);
                }
            }

            // start new defs at this step
            if (_defsByStart.TryGetValue(_currentStepIndex, out var defsToStart))
            {
                foreach (var def in defsToStart)
                {
                    _active.Add(new ActiveLfo(def));
                }
            }

            // advance step
            _currentStepIndex++;
            // loop index if there are definitions (optional): keep unbounded if no sequence length known
            // No explicit sequence length here - callers control start/stop
        }
    }

    private void OnPulse(int pulseIndex, int pulsesPerStep)
    {
        List<ActiveLfo> toProcess;
        lock (_sync)
        {
            if (!_isRunning || _active.Count == 0)
                return;
            toProcess = _active.ToList();
        }

        foreach (var a in toProcess)
        {
            var def = a.Def;

            // total pulses since this LFO started (including current pulse index within step)
            long totalPulseIndex = a.ElapsedPulses + pulseIndex;

            // pulses per whole note for this clock is pulsesPerStep (clock is expected to be configured with Division.Whole for whole-note grid)
            int pulsesPerWhole = pulsesPerStep;

            // compute pulses per waveform period (period spans PeriodInWholeNotes whole notes)
            int periodInWhole = Math.Max(1, def.PeriodInWholeNotes);
            long pulsesPerPeriod = (long)pulsesPerWhole * periodInWhole;
            long posInPeriod = pulsesPerPeriod > 0 ? (totalPulseIndex % pulsesPerPeriod) : 0;

            // normalized phase within period [0..1)
            double t = pulsesPerPeriod > 0 ? (posInPeriod / (double)pulsesPerPeriod) : 0.0;
            t += def.Phase;
            t %= 1.0;

            // produce a positive waveform that starts at 0, peaks at t=0.5 and returns to0 at t=1.0
            double p = WaveformPositive(def.Waveform, t, def.PulseWidth);
            // p in 0..1

            // pick amplitude from sequence (looped) at whole-note granularity; fallback to reasonable defaults
            int amp;
            if (def.Amplitudes != null && def.Amplitudes.Length > 0)
            {
                long wholeIndex = pulsesPerWhole > 0 ? (totalPulseIndex / pulsesPerWhole) : totalPulseIndex;
                int idx = (int)(wholeIndex % def.Amplitudes.Length);
                amp = def.Amplitudes[Math.Max(0, idx)];
            }
            else
            {
                amp = def.TargetType == LfoTargetType.ControlChange ? def.CcMax : def.PitchBendMax;
            }

            if (def.TargetType == LfoTargetType.ControlChange)
            {
                // amp assumed >=0: map p (0..1) ->0..amp
                double mapped = p * amp;
                int intVal = (int)Math.Round(mapped);
                intVal = Math.Clamp(intVal, 0, 127);

                // only send if changed
                if (a.LastSentCc == null || a.LastSentCc.Value != intVal)
                {
                    var ev = new ControlChangeEvent((SevenBitNumber)def.CcNumber, (SevenBitNumber)intVal)
                    {
                        Channel = _channel
                    };
                    _outputDevice.SendEvent(ev);
                    a.LastSentCc = intVal;
                }
            }
            else if (def.TargetType == LfoTargetType.PitchBend)
            {
                // amp can be negative or positive; treat amp as signed target displacement from center
                // map p (0..1) to signed displacement - at t=0 ->0, at t=0.5 -> sign(amp)*abs(amp), at t=1 ->0
                int signedAmp = amp;
                int displacement = (int)Math.Round(p * Math.Abs(signedAmp)) * Math.Sign(signedAmp);
                displacement = Math.Clamp(displacement, -8192, 8191);

                int raw = displacement + 8192; //0..16383
                raw = Math.Clamp(raw, 0, 16383);

                var ev = new PitchBendEvent((ushort)raw)
                {
                    Channel = _channel
                };
                _outputDevice.SendEvent(ev);
            }

            // increment elapsed pulses for this active LFO
            a.ElapsedPulses++;
        }
    }

    private static double WaveformPositive(LfoWaveform wf, double t, double pw)
    {
        // returns a value in [0..1] that starts at 0, peaks at t = 0.5 and returns to 0 at t = 1.0
        switch (wf)
        {
            case LfoWaveform.Sine:
                // sin(pi * t) ->0..1..0
                return Math.Max(0.0, Math.Sin(Math.PI * t));
            case LfoWaveform.Triangle:
                // triangle peak at0.5, linear
                return 1.0 - 2.0 * Math.Abs(t - 0.5);
            case LfoWaveform.Saw:
                // rising to peak at0.5 then drop to0 at1.0 (asymmetric saw-like)
                return t <= 0.5 ? (t / 0.5) : ((1.0 - t) / 0.5);
            case LfoWaveform.Square:
                // centered pulse at0.5 with width pw (0..1)
                double half = pw * 0.5;
                return (Math.Abs(t - 0.5) <= half) ? 1.0 : 0.0;
            default:
                return 0.0;
        }
    }

    public void Dispose()
    {
        _clockGrid.GridTick -= OnGridTick;
        _clockGrid.Pulse -= OnPulse;
    }
}
