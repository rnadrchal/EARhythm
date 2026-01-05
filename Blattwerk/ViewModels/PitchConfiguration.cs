using Prism.Mvvm;

namespace Blattwerk.ViewModels;

public abstract class PitchConfiguration : BindableBase
{
    public abstract int GetValue(float normalizedValue);
}