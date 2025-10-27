using System;
using System.Numerics;

namespace StepMutator.Common;

public static class NumericExtensions
{
    /// <summary>
    /// Toggle (invertiert) das Bit an der angegebenen Position.
    /// </summary>
    /// <typeparam name="T">Numerischer Typ</typeparam>
    /// <param name="value">Der Wert, dessen Bit getoggelt werden soll</param>
    /// <param name="position">Die Bit-Position (0 = niedrigstes Bit)</param>
    /// <returns>Neuer Wert mit getoggletem Bit</returns>
    public static T ToggleBit<T>(this T value, int position) where T : struct, INumber<T>
    {
        ulong v = Convert.ToUInt64(value);
        ulong mask = 1UL << position;
        return (T)Convert.ChangeType(v ^ mask, typeof(T));
    }

    // Existing methods...

    /// <summary>
    /// Inserts a bit at a random position. The inserted bit is randomly 0 or 1.
    /// Bits above the position are shifted left.
    /// </summary>
    public static T InsertBit<T>(this T value, Random rand) where T : struct, INumber<T>
    {
        int width = GetBitWidth<T>();
        int pos = rand.Next(width + 1); // Insert at [0, width]
        ulong v = Convert.ToUInt64(value);
        ulong bit = (ulong)rand.Next(2);
        ulong lower = v & ((1UL << pos) - 1);
        ulong upper = v & ~((1UL << pos) - 1);
        upper <<= 1;
        ulong result = lower | (bit << pos) | upper;
        // Mask to width+1 bits, then truncate to width bits
        result &= (1UL << width) - 1;
        return (T)Convert.ChangeType(result, typeof(T));
    }

    /// <summary>
    /// Deletes a bit at a random position. Bits above the position are shifted right.
    /// </summary>
    public static T DeleteBit<T>(this T value, Random rand) where T : struct, INumber<T>
    {
        int width = GetBitWidth<T>();
        if (width <= 1) return value;
        int pos = rand.Next(width); // Delete at [0, width-1]
        ulong v = Convert.ToUInt64(value);
        ulong lower = v & ((1UL << pos) - 1);
        ulong upper = (v >> (pos + 1)) << pos;
        ulong result = lower | upper;
        // Mask to width-1 bits, then truncate to width bits
        result &= (1UL << width) - 1;
        return (T)Convert.ChangeType(result, typeof(T));
    }

    /// <summary>
    /// Swaps two bits at random positions.
    /// </summary>
    public static T SwapBits<T>(this T value, Random rand) where T : struct, INumber<T>
    {
        int width = GetBitWidth<T>();
        if (width < 2) return value;
        int pos1 = rand.Next(width);
        int pos2;
        do
        {
            pos2 = rand.Next(width);
        } while (pos2 == pos1);

        ulong v = Convert.ToUInt64(value);
        ulong bit1 = (v >> pos1) & 1UL;
        ulong bit2 = (v >> pos2) & 1UL;

        if (bit1 != bit2)
        {
            v ^= (1UL << pos1) | (1UL << pos2);
        }
        return (T)Convert.ChangeType(v, typeof(T));
    }

    /// <summary>
    /// Performs a single-point crossover with another value at a random position.
    /// Returns a new value with lower bits from 'this' and upper bits from 'other'.
    /// </summary>
    public static T Crossover<T>(this T value, T other, Random rand) where T : struct, INumber<T>
    {
        int width = GetBitWidth<T>();
        int pos = rand.Next(1, width); // Crossover point [1, width-1]
        ulong v1 = Convert.ToUInt64(value);
        ulong v2 = Convert.ToUInt64(other);
        ulong maskLower = (1UL << pos) - 1;
        ulong maskUpper = ~maskLower & ((1UL << width) - 1);
        ulong result = (v1 & maskLower) | (v2 & maskUpper);
        return (T)Convert.ChangeType(result, typeof(T));
    }

    /// <summary>
    /// Inverts (reverses) a random contiguous bit segment in the value.
    /// </summary>
    public static T InvertSegment<T>(this T value, Random rand) where T : struct, INumber<T>
    {
        int width = GetBitWidth<T>();
        if (width < 2) return value;
        int start = rand.Next(width);
        int end = rand.Next(width);
        if (start > end) (start, end) = (end, start);
        if (start == end) return value;

        ulong v = Convert.ToUInt64(value);
        int len = end - start + 1;
        // Extract segment
        ulong segment = (v >> start) & ((1UL << len) - 1);
        // Reverse bits in segment
        ulong reversed = 0;
        for (int i = 0; i < len; i++)
        {
            reversed |= ((segment >> i) & 1UL) << (len - 1 - i);
        }
        // Clear original segment
        ulong mask = ((1UL << len) - 1) << start;
        v &= ~mask;
        // Insert reversed segment
        v |= (reversed << start);
        return (T)Convert.ChangeType(v, typeof(T));
    }

    /// <summary>
    /// Transposes (moves) a random contiguous bit segment to another random position.
    /// </summary>
    public static T TransposeSegment<T>(this T value, Random rand) where T : struct, INumber<T>
    {
        int width = GetBitWidth<T>();
        if (width < 2) return value;
        int start = rand.Next(width);
        int end = rand.Next(width);
        if (start > end) (start, end) = (end, start);
        if (start == end) return value;
        int len = end - start + 1;

        int target = rand.Next(width - len + 1);
        // If target overlaps segment, do nothing
        if (target >= start && target <= end) return value;

        ulong v = Convert.ToUInt64(value);
        ulong segment = (v >> start) & ((1UL << len) - 1);

        // Remove segment
        ulong lower = v & ((1UL << start) - 1);
        ulong upper = v >> (end + 1);
        ulong withoutSegment = (upper << start) | lower;

        // Insert segment at target
        ulong before = withoutSegment & ((1UL << target) - 1);
        ulong after = withoutSegment >> target;
        ulong result = before | (segment << target) | (after << (target + len));

        // Mask to width bits
        result &= (1UL << width) - 1;
        return (T)Convert.ChangeType(result, typeof(T));
    }

    public static int GetBitWidth<T>() where T : struct
    {
        if (typeof(T) == typeof(byte)) return 8;
        if (typeof(T) == typeof(short)) return 16;
        if (typeof(T) == typeof(int)) return 32;
        if (typeof(T) == typeof(long)) return 64;
        if (typeof(T) == typeof(ulong)) return 64;
        if (typeof(T) == typeof(uint)) return 32;
        // ggf. weitere Typen ergänzen
        throw new NotSupportedException($"Bitbreite für Typ {typeof(T).Name} nicht bekannt.");
    }
}