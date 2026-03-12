using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Egami.Sequencer.UI.Converters
{
    public class ValueAndContainerToWidthConverter : IMultiValueConverter
    {
        // Erwartet ConverterParameter = maxValue (z.B. "127" oder "8192")
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2) return 0.0;

            double value = 0;
            if (values[0] != null && double.TryParse(values[0].ToString(), out var v)) value = v;

            double containerWidth = 0;
            if (values[1] is double d) containerWidth = d;
            else if (values[1] is FrameworkElement fe) containerWidth = fe.ActualWidth;

            double max = 127.0;
            if (parameter != null && double.TryParse(parameter.ToString(), out var p)) max = p;

            if (max <= 0 || containerWidth <= 0) return 0.0;

            // Ziehe ein wenig Padding ab, damit Balken sichtbar bleiben
            var usable = Math.Max(4.0, containerWidth - 8.0);
            var ratio = Math.Clamp(Math.Abs(value) / max, 0.0, 1.0);
            var width = usable * ratio;
            return Math.Max(2.0, width);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}