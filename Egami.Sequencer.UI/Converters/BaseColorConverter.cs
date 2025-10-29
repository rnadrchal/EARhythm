using System.Globalization;
using System.Windows.Data;
using Egami.Imaging.Extensions;

namespace Egami.Sequencer.UI.Converters;

public class BaseColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is BaseColor baseColor)
        {
            return baseColor.ToString();
        }

        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}