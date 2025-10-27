using System;
using System.Collections.Generic;
using System.Numerics;

namespace StepMutator.Services;

public interface IMutator<T> where T : struct, INumber<T>
{
    T Mutate(T value, double rate);
    IEnumerable<T> GenerateOffspring(T parent1, T parent2, int count, IEvolutionOptions options);
}