using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Egami.Sequencer.UI.Converters;

public class NumberToMarginConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var ci = culture ?? CultureInfo.CurrentCulture;
        // 1) null -> keine Margin
        if (value == null)
            return new Thickness(0);

        // 2) Versuche, value zu double zu wandeln (unterstützt alle IConvertible-Typen)
        double number;
        try
        {
            if (value is IConvertible)
            {
                number = System.Convert.ToDouble(value, ci);
            }
            else
            {
                // Fallback: string-Repräsentation parsen
                if (!double.TryParse(value.ToString(), NumberStyles.Number, ci, out number))
                    return DependencyProperty.UnsetValue;
            }
        }
        catch
        {
            return DependencyProperty.UnsetValue;
        }

        // 3) Parameter als Faktor (optional)
        double factor = 1.0;
        if (parameter != null)
        {
            if (!double.TryParse(parameter.ToString(), NumberStyles.Number, ci, out factor))
                factor = 1.0;
        }

        var left = number * factor;
        return new Thickness(left, 0, 0, 0);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}