using System;
using System.Numerics;

namespace StepMutator.Services;

public interface IMutator<T> where T : struct, INumber<T>
{
    T Mutate(T value, double rate);
}