namespace Egami.Sequencer.Grid;

public sealed class MidiClockGrid
{
    private int _pulsesPerStep;
    private int _pulseCounter;

    public event Action? GridTick;

    /// <summary>
    /// Raised for every incoming MIDI clock pulse. Parameters: (pulseIndexWithinStep, pulsesPerStep)
    /// pulseIndexWithinStep ranges from0 .. pulsesPerStep-1
    /// </summary>
    public event Action<int,int>? Pulse;

    public GridDivision Division { get; private set; }

    public MidiClockGrid(GridDivision division)
    {
        Division = division;
        _pulsesPerStep = division.GetPulsesPerStep();
        _pulseCounter = 0;
    }
    /// <summary>
    /// Grid-Auflösung zur Laufzeit ändern.
    /// resetPhase = true: Phase zurücksetzen (neuer Takt beginnt quasi jetzt).
    /// </summary>
    public void SetDivision(GridDivision division, bool resetPhase = true)
    {
        Division = division;
        _pulsesPerStep = division.GetPulsesPerStep();

        if (resetPhase)
            _pulseCounter = 0;
    }
    public void OnClockPulse()
    {
        _pulseCounter++;

        // compute pulse index within current step (0..pulsesPerStep-1)
        int pulseIndex = (_pulseCounter -1) % _pulsesPerStep;
        if (pulseIndex <0) pulseIndex += _pulsesPerStep;

        Pulse?.Invoke(pulseIndex, _pulsesPerStep);

        if (_pulseCounter >= _pulsesPerStep)
        {
            _pulseCounter -= _pulsesPerStep;
            GridTick?.Invoke();
        }
    }

    public void Reset()
    {
        _pulseCounter = 0;
    }
}