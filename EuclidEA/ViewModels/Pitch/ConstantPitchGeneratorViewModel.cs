using Egami.Pitch;

namespace EuclidEA.ViewModels.Pitch;

public class ConstantPitchGeneratorViewModel : PitchGeneratorViewModel
{
    public ConstantPitchGeneratorViewModel(ConstantPitchGenerator generator)
    : base(generator)
    {
    }

    public override string Name => "Constant";
}