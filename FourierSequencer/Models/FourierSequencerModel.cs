using Egami.Rhythm.Midi;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using Prism.Mvvm;
using Syncfusion.Windows.Shared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Egami.Pitch;
using FourierSequencer.Events;
using Prism.Events;

namespace FourierSequencer.Models;

public class FourierSequencerModel : BindableBase
{
    private const double MinMidiValue =0.0;
    private const double MaxMidiValue =127.0;
    private const double MinThresholdSeparation =12.0; // one octave

    private readonly SequencerTarget _target;
    private readonly IEventAggregator _eventAggregator;

    public FourierSequencerModel(SequencerTarget target, IEventAggregator eventAggregator)
    {
        _target = target;
        _eventAggregator = eventAggregator;
        // initialize collections so Generate can populate them safely
        CurvePoints = new ObservableCollection<Point>();
        SamplePoints = new ObservableCollection<Point>();
        Samples = new ObservableCollection<double>();
        StepPoints = new ObservableCollection<Point>();
        StepPointsMidi = new ObservableCollection<Point>();
        MidiValues = new ObservableCollection<int>();

        // threshold line collections
        ThresholdLowerLine = new ObservableCollection<Point>();
        ThresholdUpperLine = new ObservableCollection<Point>();

        // ensure there is at least the a0 coefficient
        var coeff = new FourierCoeffizients();
        coeff.PropertyChanged += OnCoeffPropertyChanged;
        FourierCoeffizients.Add(coeff);

        // default mapping/min-max/thresholds
        MidiMin = MinMidiValue;
        MidiMax = MaxMidiValue;
        LowerThreshold = MidiMin;
        UpperThreshold = MidiMax;

        // generate initial curve (now always scaled to MIDI-range and consider min/max in mapping)
        Generate();

        MidiDevices.Input.EventReceived += OnMidiEventReceived;
        ToggleLegatoCommand = new DelegateCommand(_ => Legato = !Legato);
        ToggleActiveCommand = new DelegateCommand(_ => IsActive = !IsActive);

        _eventAggregator.GetEvent<VelocityEvent>().Subscribe(v => _velocity = v);
    }

    public SequencerTarget Target => _target;

    private bool _isActive = false;

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    private byte _velocity = 60;

    private int _harmonics = 1;
    public int Harmonics
    {
        get => _harmonics;
        set
        {
            if (value is >=0 and <16)
            {
                SetProperty(ref _harmonics, value);
                if (value < FourierCoeffizients.Count)
                {
                    // trim excess coeffs
                    while (FourierCoeffizients.Count > value +1)
                    {
                        var lastIndex = FourierCoeffizients.Count -1;
                        FourierCoeffizients[lastIndex].PropertyChanged -= OnCoeffPropertyChanged;
                        FourierCoeffizients.RemoveAt(lastIndex);
                    }
                }
                else
                {
                    // add missing coeffs
                    var n = FourierCoeffizients.Count;
                    while (FourierCoeffizients.Count < value +1)
                    {
                        var coeffizients = new FourierCoeffizients(++n);
                        coeffizients.PropertyChanged += OnCoeffPropertyChanged;
                        FourierCoeffizients.Add(coeffizients);

                        // if we just added the first harmonic (index1), set a sensible default so user sees a sine
                        var addedIndex = FourierCoeffizients.Count -1;
                        if (addedIndex ==1)
                        {
                            FourierCoeffizients[addedIndex].B =1.0;
                        }
                    }
                }
                Generate();
            }
        }
    }

    private void OnCoeffPropertyChanged(object? o, PropertyChangedEventArgs propertyChangedEventArgs)
    {
        Generate();
    }

    private int _periods =1;
    public int Periods
    {
        get => _periods;
        set
        {
            if (value is >0 and <9)
            {
                SetProperty(ref _periods, value);
                Generate();
            }
        }
    }

    private int _steps =16;
    public int Steps
    {
        get => _steps;
        set
        {
            if (value is >0 and <129)
            {
                
                if (SetProperty(ref _steps, value))
                {
                    if (_step >= value) _step = value - 1;
                    Generate();
                }
            }
        }
    }

    private int _pointsPerStep =8;
    public int PointsPerStep
    {
        get => _pointsPerStep;
        set
        {
            if (value is >0 and <65)
            {
                SetProperty(ref _pointsPerStep, value);
                Generate();
            }
        }
    }

    private double _stepOffset =0.0;
    public double StepOffset
    {
        get => _stepOffset;
        set
        {
            if (value >=0.0 && value <1.0)
            {
                SetProperty(ref _stepOffset, value);
                Generate();
            }
        }
    }

    // Observable collections are better for binding updates from the synth directly
    public ObservableCollection<Point> CurvePoints { get; private set; }

    public ObservableCollection<Point> SamplePoints { get; private set; }

    // samples as observable doubles (will hold MIDI-scaled0..127 values)
    public ObservableCollection<double> Samples { get; private set; }

    // StepPoints: points on the continuous curve corresponding to fractional step positions (s + StepOffset)
    // Y will be in MIDI range0..127 (so points are directly usable as MIDI values)
    public ObservableCollection<Point> StepPoints { get; private set; }

    // StepPoints mapped to MIDI Y range (0..127.0) - alias of StepPoints in this mode
    public ObservableCollection<Point> StepPointsMidi { get; private set; }

    // integer MIDI values for each step (0..127)
    public ObservableCollection<int> MidiValues { get; private set; }

    // Observable collections for threshold visualization
    public ObservableCollection<Point> ThresholdLowerLine { get; private set; }
    public ObservableCollection<Point> ThresholdUpperLine { get; private set; }

    public ObservableCollection<FourierCoeffizients> FourierCoeffizients { get; } = new();

    private double _midiMin;
    /// <summary>
    /// Minimum MIDI value for final scaling (default0)
    /// Curve is scaled into [MidiMin .. MidiMax]
    /// </summary>
    public double MidiMin
    {
        get => _midiMin;
        set
        {
            var v = value;
            if (double.IsNaN(v) || double.IsInfinity(v)) v = MinMidiValue;
            v = Math.Max(MinMidiValue, Math.Min(MaxMidiValue - 12, v));
            if (v > MidiMax - 12)
            {
                return;
            }
            if (SetProperty(ref _midiMin, v))
            {
                // do not change thresholds automatically; only ensure Generate updates curve
                Generate();
            }
        }
    }

    private double _midiMax;
    /// <summary>
    /// Maximum MIDI value for final scaling (default127)
    /// </summary>
    public double MidiMax
    {
        get => _midiMax;
        set
        {
            var v = value;
            if (double.IsNaN(v) || double.IsInfinity(v)) v = MaxMidiValue;
            v = Math.Max(MinMidiValue + 12, Math.Min(MaxMidiValue, v));
            if (v < MidiMin + 12)
            {
                return;
            }
            if (SetProperty(ref _midiMax, v))
            {
                // do not change thresholds automatically; only regenerate curve
                Generate();
            }
        }
    }

    private double _lowerThreshold;
    /// <summary>
    /// Lower horizontal threshold (display only). Enforced to be at least MinThresholdSeparation below UpperThreshold.
    /// </summary>
    public double LowerThreshold
    {
        get => _lowerThreshold;
        set
        {
            var v = value;
            if (double.IsNaN(v) || double.IsInfinity(v)) v = 0;
            // enforce minimum separation to UpperThreshold
            if (UpperThreshold - v < MinThresholdSeparation)
            {
                v = UpperThreshold - MinThresholdSeparation;
            }

            if (SetProperty(ref _lowerThreshold, v))
            {
                UpdateThresholdLines();
            }
        }
    }

    private double _upperThreshold;
    /// <summary>
    /// Upper horizontal threshold (display only). Enforced to be at least MinThresholdSeparation above LowerThreshold.
    /// </summary>
    public double UpperThreshold
    {
        get => _upperThreshold;
        set
        {
            var v = value;
            if (double.IsNaN(v) || double.IsInfinity(v)) v = 127;

            // enforce minimum separation to LowerThreshold
            if (v - LowerThreshold < MinThresholdSeparation)
            {
                v = LowerThreshold + MinThresholdSeparation;
            }

            if (SetProperty(ref _upperThreshold, v))
            {
                UpdateThresholdLines();
            }
        }
    }

    private bool _ledBeat;
    
    public bool LedBeat
    {
        get => _ledBeat;
        set => SetProperty(ref _ledBeat, value);
    }

    private static int[] _dividers = [1, 2, 3, 4, 6, 8, 12, 16, 24, 32, 48, 96];
    private int _divider = 8;
    public int Divider
    {
        get => _divider;
        set
        {
            if (_dividers.Contains(value))
            {
                SetProperty(ref _divider, value);
            }
        }
    }

    private int _channel = 0;

    public int Channel
    {
        get => _channel;
        set
        {
            if (value is >= 0 and < 16)
            {
                SetProperty(ref _channel, value);
            }
        }
    }

    private bool _legato;

    public bool Legato
    {
        get => _legato;
        set
        {
            if (SetProperty(ref _legato, value))
            {
                SendNoteOff();
            }

        }
    }

    public ICommand ToggleLegatoCommand { get; }

    public ICommand ToggleActiveCommand { get; }


    public void Generate()
    {
        int totalPoints = _steps * _pointsPerStep;

        // compute raw dense curve values first so we can scale to MIDI range
        var rawXs = new double[totalPoints];
        var rawYs = new double[totalPoints];

        for (int i =0; i < totalPoints; i++)
        {
            double x = (double)i / _pointsPerStep; //0 .. steps (not inclusive of final point)
            double t = x / _steps; // fraction0..1
            double angle = t *2.0 * Math.PI * _periods;

            double y =0.0;
            // a0 term is aCoeffs[0]/2
            if (FourierCoeffizients.Count >0)
                y += GetA(0) /2.0;

            for (int n =1; n <= _harmonics; n++)
            {
                double a = GetA(n);
                double b = GetB(n);
                y += a * Math.Cos(n * angle) + b * Math.Sin(n * angle);
            }

            rawXs[i] = x;
            rawYs[i] = y;
        }

        // determine peak for scaling (use dense curve)
        double peak =0.0;
        for (int i =0; i < rawYs.Length; i++)
        {
            var abs = Math.Abs(rawYs[i]);
            if (abs > peak) peak = abs;
        }
        if (peak <=0.0) peak =1.0;

        // populate CurvePoints, scaled into [MidiMin..MidiMax]
        CurvePoints.Clear();
        double range = MidiMax - MidiMin;
        for (int i =0; i < totalPoints; i++)
        {
            double a = rawYs[i];
            if (a > peak) a = peak;
            if (a < -peak) a = -peak;
            double norm = (a + peak) / (2.0 * peak); //0..1
            double midiDouble = MidiMin + norm * range;
            CurvePoints.Add(new Point(rawXs[i], midiDouble));
        }

        // populate discrete samples and step points, mapping Y to [MidiMin..MidiMax] using same peak
        SamplePoints.Clear();
        Samples.Clear();
        StepPoints.Clear();
        MidiValues.Clear();
        StepPointsMidi.Clear();

        for (int s =0; s < _steps; s++)
        {
            double x = s + _stepOffset; // in step-space (fractional position for step point)
            double t = x / _steps; //0..1
            double angle = t *2.0 * Math.PI * _periods;

            double y =0.0;
            if (FourierCoeffizients.Count >0)
                y += GetA(0) /2.0;

            for (int n =1; n <= _harmonics; n++)
            {
                double a = GetA(n);
                double b = GetB(n);
                y += a * Math.Cos(n * angle) + b * Math.Sin(n * angle);
            }

            // map y to MIDI range using same peak
            double clamped = y;
            if (clamped > peak) clamped = peak;
            if (clamped < -peak) clamped = -peak;
            double normSample = (clamped + peak) / (2.0 * peak);
            double midiDoubleSample = MidiMin + normSample * range;
            int midiInt = (int)Math.Round(midiDoubleSample);

            Samples.Add(midiDoubleSample); // Samples now hold MIDI-scaled double values
            SamplePoints.Add(new Point(s, midiDoubleSample));
            StepPoints.Add(new Point(x, midiDoubleSample));
            StepPointsMidi.Add(new Point(x, midiDoubleSample));
            MidiValues.Add(midiInt);
        }

        // update threshold display lines
        UpdateThresholdLines();
    }

    private void UpdateThresholdLines()
    {
        ThresholdLowerLine.Clear();
        ThresholdUpperLine.Clear();

        // Two points spanning X from0 to Steps
        ThresholdLowerLine.Add(new Point(0, LowerThreshold));
        ThresholdLowerLine.Add(new Point(_steps, LowerThreshold));

        ThresholdUpperLine.Add(new Point(0, UpperThreshold));
        ThresholdUpperLine.Add(new Point(_steps, UpperThreshold));
    }

    private double GetA(int n) => FourierCoeffizients.Count > n ? FourierCoeffizients[n].A :0.0;
    private double GetB(int n) => FourierCoeffizients.Count > n ? FourierCoeffizients[n].B :0.0;

    private ulong _tickCount = 0;
    private int _step = 0;
    private byte? _activeValue;
    private void OnMidiEventReceived(object sender, MidiEventReceivedEventArgs e)
    {
        if (e.Event.EventType is MidiEventType.Start)
        {
            _tickCount = 0;
        }

        if (e.Event.EventType is MidiEventType.Stop)
        {
            LedBeat = false;
        }

        if (_activeValue != null && _target == SequencerTarget.Pitch)
        {
            SendNoteOff();
        }

        if (!_isActive) return;

        if (e.Event.EventType is MidiEventType.TimingClock)
        {
            if (_tickCount % 24 == 0)
            {
                LedBeat = true;
            }
            else if (_tickCount % 24 == 12)
            {
                LedBeat = false;
            }

            if (_tickCount % (ulong)(96 / _divider) == 0)
            {
                var value = (byte)(MidiValues[_step++]);
                if (value >= LowerThreshold && value <= UpperThreshold)
                {
                    switch (_target)
                    {
                        case SequencerTarget.Pitch:
                            SendNoteOn(value, _velocity);
                            break;
                        case SequencerTarget.Velocity:
                            SendVelocity(value);
                            break;
                        case SequencerTarget.Pitchbend:
                            SendPitchbend(value);
                            break;
                        case SequencerTarget.ControlChange:
                            SendCc1(value);
                            break;
                    }
                }

                if (_step >= MidiValues.Count)
                {
                    _step = 0;
                }
            }
            _tickCount++;
        }
    }


    public void SendNoteOn(byte pitch, byte velocity)
    {
        if (!Legato && _activeValue == pitch)
        {
            SendNoteOff();
        }
        if (Legato && _activeValue == pitch)
        {
            return;
        }
        MidiDevices.Output.SendEvent(new NoteOnEvent((SevenBitNumber)pitch, (SevenBitNumber)velocity) { Channel = (FourBitNumber)_channel });
        _activeValue = pitch;
    }

    public void SendNoteOff()
    {
        if (_activeValue.HasValue)
        {
            MidiDevices.Output.SendEvent(new NoteOffEvent((SevenBitNumber)_activeValue.Value, (SevenBitNumber)0) { Channel = (FourBitNumber)_channel });
            _activeValue = null;
        }
    }

    public void SendVelocity(byte velocity)
    {
        _activeValue = velocity;
        _eventAggregator.GetEvent<VelocityEvent>().Publish(velocity);
    }

    public void SendPitchbend(byte pitchbend)
    {
        _activeValue = pitchbend;
        ushort result = (ushort)Math.Min(16383, pitchbend * 128);
        MidiDevices.Output.SendEvent(new PitchBendEvent(result) { Channel = (FourBitNumber)_channel});
    }

    public void SendCc1(byte value)
    {
        _activeValue = value;
        MidiDevices.Output.SendEvent(new ControlChangeEvent((SevenBitNumber)1, (SevenBitNumber)value) { Channel = (FourBitNumber) _channel });
    }

    public void Cleanup()
    {
        if (_activeValue != null)
        {
            switch (_target)
            {
                case SequencerTarget.Pitch:
                    SendNoteOff();
                    break;
                case SequencerTarget.Pitchbend:
                    MidiDevices.Output.SendEvent(new PitchBendEvent((ushort)(16383 / 2)));
                    break;
                case SequencerTarget.ControlChange:
                    MidiDevices.Output.SendEvent(new ControlChangeEvent((SevenBitNumber)1, (SevenBitNumber)0));
                    break;
                case SequencerTarget.Velocity:
                    _eventAggregator.GetEvent<VelocityEvent>().Publish(0);
                    break;
            }
        }
    }

}