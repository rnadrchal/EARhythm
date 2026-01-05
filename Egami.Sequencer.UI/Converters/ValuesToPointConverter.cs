using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Egami.Sequencer.UI.Converters
{
    public class ValuesToPointsConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 3) return new PointCollection();

            // Werte-Array extrahieren (robust gegenüber IEnumerable unterschiedlicher numerischer Typen)
            float[] vals = null;
            if (values[0] is float[] fa) vals = fa;
            else if (values[0] is System.Collections.IEnumerable ie)
            {
                var tmp = new List<float>();
                foreach (var o in ie)
                {
                    try
                    {
                        // Convert.ToSingle übernimmt viele Typen und respektiert CultureInfo nicht direkt,
                        // deshalb versuchen wir String-Parsing separat.
                        if (o is string s)
                        {
                            if (float.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, culture, out var pf))
                                tmp.Add(pf);
                        }
                        else
                        {
                            tmp.Add(System.Convert.ToSingle(o));
                        }
                    }
                    catch
                    {
                        // ignorieren unkonvertierbarer Einträge
                    }
                }
                vals = tmp.ToArray();
            }

            if (vals == null || vals.Length == 0) return new PointCollection();

            if (!double.TryParse(values[1]?.ToString(), NumberStyles.Float, culture, out var width)) width = 0;
            if (!double.TryParse(values[2]?.ToString(), NumberStyles.Float, culture, out var height)) height = 0;

            var points = new PointCollection(Math.Max(0, vals.Length * 2));

            double segmentWidth = vals.Length > 0 ? width / vals.Length : 0;

            for (int i = 0; i < vals.Length; i++)
            {
                double x0 = i * segmentWidth;
                double x1 = (i + 1) * segmentWidth;
                double clamped = Math.Clamp(vals[i], 0f, 1f);
                double y = (1.0 - clamped) * height;

                // horizontale Linie für Segment i: (x0,y) -> (x1,y)
                points.Add(new Point(x0, y));
                points.Add(new Point(x1, y));
            }

            return points;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}