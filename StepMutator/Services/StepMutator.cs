using System;
using System.Numerics;
using StepMutator.Common;

namespace StepMutator.Services;

public class StepMutator<T> : IMutator<T> where T : struct, INumber<T>
{
    private readonly IEvolutionOptions _options;
    private readonly Random _random;

    public StepMutator(IEvolutionOptions options)
    {
        _options = options;
        _random = RandomProvider.Get(_options.Seed);
    }

    public T Mutate(T value, double rate)
    {
        T result = value;
        if (_random.NextDouble() <= rate)
        {
            var bitWidth = NumericExtensions.GetBitWidth<T>();
            var position = RandomProvider.Get(_options.Seed).Next(0, bitWidth);

            result = result.ToggleBit(position);
        }

        if (_random.NextDouble() <= _options.DeletionRate)
        {
            result = result.DeleteBit(_random);
        }

        if (_random.NextDouble() <= _options.InsertionRate)
        {
            result = result.InsertBit(_random);
        }

        if (_random.NextDouble() <= _options.SwapRate)
        {
            result = result.SwapBits(_random);
        }

        if (_random.NextDouble() <= _options.InversionRate)
        {
            result = result.InvertSegment(_random);
        }

        if (_random.NextDouble() <= _options.TranspositionRate)
        {
            result = result.TransposeSegment(_random);
        }

        return result;
    }

}