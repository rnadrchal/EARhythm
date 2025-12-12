using Melanchall.DryWetMidi.Common;

namespace Egami.Sequencer.Automation;

public class AutomationParameter
{
    private readonly object _sync = new();

    public string Name { get; }
    public float Min { get; }
    public float Max { get; }

    private float _value;
    public float Value
    {
        get
        {
            lock (_sync) return _value;
        }
        private set
        {
            lock (_sync)
            {
                var clamped = Math.Clamp(value, Min, Max);
                if (Math.Abs(clamped - _value) < 0.0001f)
                    return;

                _value = clamped;
                OnValueChanged?.Invoke(_value);
            }
        }
    }

    /// <summary>
    /// Wird aufgerufen, wenn sich der Wert ändert.
    /// </summary>
    public event Action<float>? OnValueChanged;

    /// <summary>
    /// Optional: Mapping von 0..127 auf einen Parameterwert.
    /// Default: linear Min..Max.
    /// </summary>
    public Func<SevenBitNumber, float>? CcToValueMapper { get; set; }

    public AutomationParameter(string name, float min, float max, float initial)
    {
        if (max <= min) throw new ArgumentException("max must be > min");

        Name = name;
        Min = min;
        Max = max;

        _value = Math.Clamp(initial, min, max);
    }

    public void SetFromCc(SevenBitNumber ccValue)
    {
        var v = CcToValueMapper != null
            ? CcToValueMapper(ccValue)
            : Min + (Max - Min) * (ccValue / 127f);

        Value = v;
    }

    /// <summary>
    /// Manuelles Setzen in „Realwerten“ (z. B. direkt Transpose-Semitones).
    /// </summary>
    public void Set(float value) => Value = value;
}