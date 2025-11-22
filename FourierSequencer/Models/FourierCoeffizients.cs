using Prism.Mvvm;

namespace FourierSequencer.Models;

public class FourierCoeffizients : BindableBase
{
    private readonly int _number;
    public int Number => _number;

    private double _a = 0.0;
    public double A
    {
        get => _a;
        set
        {
            if (value is >= -1.0 and <= 1.0)
            {
                SetProperty(ref _a, value);
            }
        }
    }

    private double _b = 0.0;

    public FourierCoeffizients(int number = 1)
    {
        _number = number;
    }

    public double B
    {
        get => _b;
        set
        {
            if (value is >= -1.0 and <= 1.0)
            {
                SetProperty(ref _b, value);
            }
        }
    }
}