using System.Numerics;

namespace Egami.Sequencer.Common
{
    public enum MeanType
            {
        Arithmetic,
        Geometric,
        Harmonic
    }
    /// <summary>
    /// Numeric helper utilities.
    /// </summary>
    public static class NumericUtils
    {
        /// <summary>
        /// Maps a value from one range to another: (xmin..xmax) -> (ymin..ymax).
        /// Generic implementation using <see cref="INumber{T}"/> (requires .NET7+ / .NET9).
        /// Note: when x is outside the source range the result will be outside the target range
        /// (no clamping is performed).
        /// </summary>
        public static T Map<T>(T x, T xmin, T xmax, T ymin, T ymax)
        where T : INumber<T>
        {
            if (xmin == xmax) throw new ArgumentException("Source range must not be zero.", nameof(xmin));

            // ratio = (x - xmin) / (xmax - xmin)
            T ratio = (x - xmin) / (xmax - xmin);
            return ymin + ratio * (ymax - ymin);
        }

        /// <summary>
        /// Maps a double value from one range to another with optional clamping to the target range.
        /// </summary>
        public static double Map(double x, double xmin, double xmax, double ymin, double ymax, bool clamp = false)
        {
            if (double.IsNaN(x) || double.IsNaN(xmin) || double.IsNaN(xmax) || double.IsNaN(ymin) || double.IsNaN(ymax))
                throw new ArgumentException("Numeric arguments must not be NaN.");

            if (xmin == xmax) throw new ArgumentException("Source range must not be zero.", nameof(xmin));

            double ratio = (x - xmin) / (xmax - xmin);
            double y = ymin + ratio * (ymax - ymin);

            if (!clamp) return y;

            if (ymin <= ymax)
                return Math.Min(Math.Max(y, ymin), ymax);
            else
                return Math.Min(Math.Max(y, ymax), ymin);
        }

        /// <summary>
        /// Computes the geometric mean of a sequence of positive doubles.
        /// Uses the log-sum-exp approach to avoid overflow/underflow: exp(average(log(x))).
        /// Throws ArgumentException if sequence is empty or contains non-positive values.
        /// </summary>
        public static double GeometricMean(IEnumerable<double> values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            // filter out NaN
            var arr = values.Where(d => !double.IsNaN(d)).ToArray();
            if (arr.Length == 0) throw new ArgumentException("Sequence contains no values", nameof(values));
            // geometric mean defined for positive values only
            if (arr.Any(d => d <= 0.0)) throw new ArgumentException("Geometric mean requires all values to be >0.", nameof(values));

            double sumLog = 0.0;
            foreach (var v in arr)
            {
                sumLog += Math.Log(v);
            }
            return Math.Exp(sumLog / arr.Length);
        }

        /// <summary>
        /// Computes the harmonic mean of a sequence of positive doubles.
        /// Harmonic mean = n / sum(1/x). Throws ArgumentException if sequence is empty or contains non-positive values.
        /// </summary>
        public static double HarmonicMean(IEnumerable<double> values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            var arr = values.Where(d => !double.IsNaN(d)).ToArray();
            if (arr.Length == 0) throw new ArgumentException("Sequence contains no values", nameof(values));
            if (arr.Any(d => d <= 0.0)) throw new ArgumentException("Harmonic mean requires all values to be >0.", nameof(values));

            double sumRecip = 0.0;
            foreach (var v in arr)
            {
                sumRecip += 1.0 / v;
            }
            return arr.Length / sumRecip;
        }

        public static double Mean(this IEnumerable<double> values, MeanType meanType)
        {
            return meanType switch
            {
                MeanType.Arithmetic => values.Average(),
                MeanType.Geometric => GeometricMean(values),
                MeanType.Harmonic => HarmonicMean(values),
                _ => throw new ArgumentOutOfRangeException(nameof(meanType), "Unknown mean type.")
            };
        }
    }
}
