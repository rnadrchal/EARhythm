using System.Globalization;
using System.Windows.Data;

namespace Egami.Sequencer.UI.Converters;

public class BooleanToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            string result = "";
            if (parameter != null)
            {
                var s = parameter.ToString();
                var parts = s?.Split('|');
                if (parts != null && parts.Length == 2)
                {
                    result = b ? parts[0] : parts[1];
                }
                else
                {
                    result = b ? s ! : string.Empty;
                }
            }
            else
            {
                result = b ? "True" : "False";
            }
            return result;
        }
        
        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}