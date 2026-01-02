using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Egami.Sequencer.UI.Converters
{
    /// <summary>
    /// Einfacher IValueConverter: vergleicht das gebundene Value mit dem ConverterParameter (expected).
    /// Bei Gleichheit liefert er einen roten SolidColorBrush, sonst ein dunkles Grau.
    /// Unterstützt numerische Werte, Enums und Strings (Vergleich ist tolerant für numerische Typen).
    /// Usage: Foreground="{Binding SomeValue, Converter={StaticResource EqualityToBrush}, ConverterParameter=ExpectedValue}"
    /// </summary>
    public sealed class EqualityToBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush TrueBrush = CreateFrozenBrush(Colors.Red);
        private static readonly SolidColorBrush FalseBrush = CreateFrozenBrush(Color.FromRgb(64, 64, 64));

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            object? expected = parameter;

            // If expected provided as string but target is enum, try parse to enum
            if (expected is string s && value != null)
            {
                var valType = value.GetType();

                if (valType.IsEnum)
                {
                    try
                    {
                        expected = Enum.Parse(valType, s, ignoreCase: true);
                    }
                    catch
                    {
                        // keep expected as string if parse fails
                    }
                }
                else
                {
                    // try to convert string parameter to the actual value's type (numeric/text)
                    try
                    {
                        expected = TypeDescriptor.GetConverter(valType).ConvertFromString(null, CultureInfo.InvariantCulture, s);
                    }
                    catch
                    {
                        // fallback keep string
                    }
                }
            }

            bool eq = AreEqual(value, expected);

            return eq ? TrueBrush : FalseBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();

        private static bool AreEqual(object? a, object? b)
        {
            if (a is null && b is null) return true;
            if (a is null || b is null) return false;

            // direct equality
            if (a.Equals(b)) return true;

            // numeric tolerant comparison
            if (TryToDouble(a, out double da) && TryToDouble(b, out double db))
            {
                double eps = Math.Max(1e-9, Math.Abs(da) * 1e-9);
                return Math.Abs(da - db) <= Math.Max(eps, 1e-6);
            }

            // fallback: compare invariant strings, case-insensitive
            var sa = System.Convert.ToString(a, CultureInfo.InvariantCulture) ?? string.Empty;
            var sb = System.Convert.ToString(b, CultureInfo.InvariantCulture) ?? string.Empty;
            return string.Equals(sa.Trim(), sb.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryToDouble(object? v, out double d)
        {
            d = double.NaN;
            if (v == null) return false;

            switch (v)
            {
                case byte b: d = b; return true;
                case sbyte sb: d = sb; return true;
                case short s: d = s; return true;
                case ushort us: d = us; return true;
                case int i: d = i; return true;
                case uint ui: d = ui; return true;
                case long l: d = l; return true;
                case ulong ul: d = ul; return true;
                case float f: d = f; return true;
                case double db: d = db; return true;
                case decimal dec: d = (double)dec; return true;
                default:
                    if (v is IConvertible conv)
                    {
                        try
                        {
                            d = conv.ToDouble(CultureInfo.InvariantCulture);
                            return true;
                        }
                        catch { return false; }
                    }
                    return false;
            }
        }

        private static SolidColorBrush CreateFrozenBrush(Color c)
        {
            var b = new SolidColorBrush(c);
            b.Freeze();
            return b;
        }
    }
}