using Prism.Mvvm;

namespace ChemSequencer.ViewModels;

public class StepLed : BindableBase
{
    private int _index;
    public int Index => _index;
    public bool IsCurrent { get; private set; }

    public StepLed(int index)
    {
        _index = index;
    }

    public void SetCurrent(bool isCurrent)
    {
        IsCurrent = isCurrent;
        RaisePropertyChanged(nameof(IsCurrent));
    }
}