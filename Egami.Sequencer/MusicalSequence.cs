namespace Egami.Sequencer;

public sealed class MusicalSequence
{
    private readonly List<SequenceStep> _steps;
    public IReadOnlyList<SequenceStep> Steps => _steps;

    public int LengthInSteps { get; }

    public MusicalSequence(IEnumerable<SequenceStep> steps)
    {
        _steps = steps
            .OrderBy(s => s.StepIndex)
            .ToList();

        LengthInSteps = _steps.Count == 0
            ? 0
            : _steps.Max(s => s.StepIndex + s.LengthInSteps);
    }

    public static MusicalSequence Empty => new(Array.Empty<SequenceStep>());
}