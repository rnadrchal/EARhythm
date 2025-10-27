using Prism.Mvvm;
using StepMutator.Services;

namespace StepMutator.Models.Evolution;

public class EvolutionOptions : BindableBase, IEvolutionOptions
{
    public int? Seed => null;

    private int _generationLength = 100;
    public int GenerationLength
    {
        get => _generationLength;
        set => SetProperty(ref _generationLength, value);
    }
}