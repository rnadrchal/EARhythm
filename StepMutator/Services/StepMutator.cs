using System;
using System.Numerics;
using Egami.Rhythm.EA.Extensions;
using StepMutator.Common;

namespace StepMutator.Services;

public class StepMutator<T> : IMutator<T> where T : struct, INumber<T>
{
    public T Mutate(T value, double rate, IEvolutionOptions options)
    {
        if (RandomProvider.Get(options.Seed).NextDouble() <= rate)
        {
            var bitWidth = NumericExtensions.GetBitWidth<T>();
            var position = RandomProvider.Get(options.Seed).Next(0, bitWidth);

            return value.ToggleBit(position);
        }

        return value;
    }

}