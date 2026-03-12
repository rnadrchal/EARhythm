using System;
using System.Globalization;
using System.Windows.Data;

namespace Egami.Sequencer.UI.Converters
{
    public class ValueToPixelsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // robustes Numerik-Parsing
            double numeric;
            if (value is int i) numeric = i;
            else if (value is double d) numeric = d;
            else if (value is float f) numeric = f;
            else if (value is long l) numeric = l;
            else if (value is string s && double.TryParse(s, NumberStyles.Any, culture, out var sv)) numeric = sv;
            else
                return 0.0;

            string param = parameter as string;
            if (string.IsNullOrWhiteSpace(param))
                return numeric; // kein Scale angegeben -> roher Wert zurück

            var parts = param.Split(';');
            if (!double.TryParse(parts[0], NumberStyles.Any, culture, out double scale))
                return numeric;

            double min = 0.0;
            if (parts.Length > 1 && double.TryParse(parts[1], NumberStyles.Any, culture, out double parsedMin))
                min = parsedMin;

            return Math.Max(numeric * scale, min);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}