using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Navigation;

namespace ChemSequencer.ViewModels;

public class BondToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int order)
        {
            var result = order switch
            {
                1 => "—",
                2 => "=",
                3 => "≡",
                4 => "⁛", // aromatic
                _ => "?"
            };
            return result;
        }
        return "?";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}