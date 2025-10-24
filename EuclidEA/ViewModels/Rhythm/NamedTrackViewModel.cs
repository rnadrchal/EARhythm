using Egami.Rhythm.Pattern;

namespace EuclidEA.ViewModels.Rhythm;

public class NamedTrackViewModel
{
    private readonly Sequence _sequence;
    private readonly string _name;

    public NamedTrackViewModel(Sequence sequence, string name)
    {
        _sequence = sequence;
        _name = name;
    }

    public string Name => _name;
    public Sequence Sequence => _sequence;
    public int SequenceLength => _sequence.StepsTotal;
}