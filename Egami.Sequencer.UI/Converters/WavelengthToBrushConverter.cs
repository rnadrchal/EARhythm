using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Egami.Sequencer.UI.Converters
{
    /// <summary>
    /// Konvertiert eine Wellenlänge in nm (double/int/string) in eine sichtbare RGB-Farbe (Brush).
    /// Bereich 380..780 nm. Außerhalb dieses Bereichs wird Transparent zurückgegeben.
    /// Implementiert eine bekannte Näherung (Dan Bruton) mit Gamma- und Intensitätskorrektur.
    /// </summary>
    public sealed class WavelengthToBrushConverter : IValueConverter
    {
        public double Gamma { get; set; } = 0.8;

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null) return null;

            if (!TryGetWavelength(value, out double wl)) return null;

            if (wl < 380.0 || wl > 780.0)
                return Brushes.Transparent;

            WavelengthToRgb(wl, out double r, out double g, out double b, Gamma);

            byte rb = (byte)Math.Clamp((int)Math.Round(r * 255.0), 0, 255);
            byte gb = (byte)Math.Clamp((int)Math.Round(g * 255.0), 0, 255);
            byte bb = (byte)Math.Clamp((int)Math.Round(b * 255.0), 0, 255);

            var brush = new SolidColorBrush(Color.FromArgb(255, rb, gb, bb));
            brush.Freeze();
            return brush;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();

        private static bool TryGetWavelength(object value, out double wavelength)
        {
            switch (value)
            {
                case double d:
                    wavelength = d;
                    return true;
                case float f:
                    wavelength = f;
                    return true;
                case int i:
                    wavelength = i;
                    return true;
                case long l:
                    wavelength = l;
                    return true;
                case string s when double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var ds):
                    wavelength = ds;
                    return true;
                default:
                    wavelength = 0;
                    return false;
            }
        }

        // Näherung: gibt r,g,b in 0..1 zurück (Dan Bruton's algorithm)
        private static void WavelengthToRgb(double wavelength, out double r, out double g, out double b, double gamma)
        {
            // Basis-RGB gemäß Wellenlänge
            if (wavelength >= 380 && wavelength <= 440)
            {
                r = -(wavelength - 440) / (440 - 380);
                g = 0.0;
                b = 1.0;
            }
            else if (wavelength > 440 && wavelength <= 490)
            {
                r = 0.0;
                g = (wavelength - 440) / (490 - 440);
                b = 1.0;
            }
            else if (wavelength > 490 && wavelength <= 510)
            {
                r = 0.0;
                g = 1.0;
                b = -(wavelength - 510) / (510 - 490);
            }
            else if (wavelength > 510 && wavelength <= 580)
            {
                r = (wavelength - 510) / (580 - 510);
                g = 1.0;
                b = 0.0;
            }
            else if (wavelength > 580 && wavelength <= 645)
            {
                r = 1.0;
                g = -(wavelength - 645) / (645 - 580);
                b = 0.0;
            }
            else if (wavelength > 645 && wavelength <= 780)
            {
                r = 1.0;
                g = 0.0;
                b = 0.0;
            }
            else
            {
                r = g = b = 0.0;
            }

            // Intensitätsfaktor an den Rändern
            double factor;
            if (wavelength >= 380 && wavelength <= 420)
                factor = 0.3 + 0.7 * (wavelength - 380) / (420 - 380);
            else if (wavelength > 700 && wavelength <= 780)
                factor = 0.3 + 0.7 * (780 - wavelength) / (780 - 700);
            else if (wavelength > 420 && wavelength <= 700)
                factor = 1.0;
            else
                factor = 0.0;

            // Gamma-Korrektur + Faktor anwenden
            r = Math.Pow(Math.Clamp(r * factor, 0.0, 1.0), gamma);
            g = Math.Pow(Math.Clamp(g * factor, 0.0, 1.0), gamma);
            b = Math.Pow(Math.Clamp(b * factor, 0.0, 1.0), gamma);
        }
    }
}