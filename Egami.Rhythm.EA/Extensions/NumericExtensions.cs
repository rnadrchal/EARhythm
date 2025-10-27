using System.Numerics;

namespace Egami.Rhythm.EA.Extensions;

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