namespace Egami.EA.Metrics.Util;

public static class MathUtil
{
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static double Clamp01(double x) => x < 0 ? 0 : (x > 1 ? 1 : x);

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static int Abs(int v) => v < 0 ? -v : v;
}