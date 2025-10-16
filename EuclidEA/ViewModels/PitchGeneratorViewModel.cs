using Egami.Pitch;
using Prism.Mvvm;

namespace EuclidEA.ViewModels;

public abstract class PitchGeneratorViewModel : BindableBase, IPitchGeneratorViewModel
{
    private readonly IPitchGenerator _generator;
    public abstract string Name { get; }

    private int _octave = 0;
    public int Octave
    {
        get => _octave;
        set => SetProperty(ref _octave, value);
    }

    private int _note = 0; // Middle C

    protected PitchGeneratorViewModel(IPitchGenerator generator)
    {
        _generator = generator;
    }

    public int Note
    {
        get => _note;
        set => SetProperty(ref _note, value);
    }

    protected int NoteNumber => _note + ((_octave + 4) * 12);

    public virtual byte?[] Generate(int length)
    {
        return _generator.Generate((byte)NoteNumber, length);
    }
}