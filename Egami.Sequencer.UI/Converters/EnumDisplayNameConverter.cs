using System.Collections.Concurrent;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;

namespace Egami.Sequencer.UI.Converters
{
    public sealed class EnumDisplayNameConverter : IValueConverter
    {
        // Cache: Type -> (enum value -> display string)
        private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<object, string>> _cache
            = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is null) return null;

            var enumType = value.GetType();
            if (!enumType.IsEnum) return value.ToString();

            var map = _cache.GetOrAdd(enumType, t => BuildMap(t));
            return map.TryGetValue(value, out var s) ? s : value.ToString();
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            if (targetType.IsEnum == false && Nullable.GetUnderlyingType(targetType)?.IsEnum != true)
                throw new InvalidOperationException("ConvertBack targetType must be an enum or nullable enum.");

            var enumType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            var map = _cache.GetOrAdd(enumType, t => BuildMap(t));

            // try exact match first
            var match = map.FirstOrDefault(kv => string.Equals(kv.Value, value.ToString(), StringComparison.CurrentCulture));
            if (!match.Equals(default(KeyValuePair<object, string>)))
                return Enum.ToObject(enumType, match.Key);

            // fallback: try parse by name
            if (Enum.TryParse(enumType, value.ToString(), out var parsed))
                return parsed!;

            throw new InvalidOperationException($"Unable to convert '{value}' to enum {enumType.Name}.");
        }

        private static ConcurrentDictionary<object, string> BuildMap(Type enumType)
        {
            var dict = new ConcurrentDictionary<object, string>();

            foreach (var field in enumType.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var rawValue = field.GetValue(null)!;

                // Prefer DisplayAttribute
                var display = field.GetCustomAttribute<DisplayAttribute>();
                if (display != null)
                {
                    var name = display.GetName();
                    if (!string.IsNullOrEmpty(name))
                    {
                        dict[rawValue] = name;
                        continue;
                    }
                }

                // Fallback to DescriptionAttribute
                var desc = field.GetCustomAttribute<DescriptionAttribute>();
                if (desc != null && !string.IsNullOrEmpty(desc.Description))
                {
                    dict[rawValue] = desc.Description!;
                    continue;
                }

                // Fallback to the enum name itself
                dict[rawValue] = field.Name;
            }

            return dict;
        }
    }
}