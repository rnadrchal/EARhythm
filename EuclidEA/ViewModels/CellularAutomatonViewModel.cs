using Egami.Rhythm.Generation;

namespace EuclidEA.ViewModels;

public class CellularAutomatonViewModel : RhythmGeneratorViewModel
{
    protected override IRhythmGenerator Generator => new CellularAutomatonGenerator();
    public override string Name => "Cellular Automaton";
}