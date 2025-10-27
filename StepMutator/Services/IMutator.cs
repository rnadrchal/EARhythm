using System;
using System.Numerics;

namespace StepMutator.Services;

public interface IMutator<T> where T : struct, INumber<T>
{
    T Mutate(T value, double rate);
    T GenerateOffspring(T parent1, T parent2, IEvolutionOptions options);
}