using System;
using System.Numerics;

namespace TextSequencer.Services
{
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
    }
}
