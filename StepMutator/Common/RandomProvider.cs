using System;

namespace StepMutator.Common;

public static class RandomProvider
{
    public static Random Get(int? seed = null)
        => seed.HasValue ? new Random(seed.Value) : Random.Shared;
}