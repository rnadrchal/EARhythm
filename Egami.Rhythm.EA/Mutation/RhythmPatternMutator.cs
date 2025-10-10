using Egami.Rhythm.Common;
using Egami.Rhythm.Pattern;

namespace Egami.Rhythm.EA.Mutation;

public class RhythmPatternMutator : IMutator<RhythmPattern>
{
    public void Mutate(RhythmPattern individual, EvolutionContext ctx)
    {
        var position = RandomProvider.Get(ctx.Seed).Next(0, individual.Hits.Length);
        individual.Hits[position] = !individual.Hits[position];
    }

    public void Delete(RhythmPattern individual, EvolutionContext ctx)
    {
        var position = RandomProvider.Get(ctx.Seed).Next(0, individual.Hits.Length);
        var hits = individual.Hits.ToList();
        hits.RemoveAt(position);
        var lengths = individual.Lengths.ToList();
        lengths.RemoveAt(position);
        var velocity = individual.Velocity.ToList();
        velocity.RemoveAt(position);
        individual.StepsTotal--;
        individual.Hits = hits.ToArray();
        individual.Lengths = lengths.ToArray();
        individual.Velocity = velocity.ToArray();
    }

    public void Insert(RhythmPattern individual, EvolutionContext ctx)
    {
        var position = RandomProvider.Get(ctx.Seed).Next(0, individual.Hits.Length);
        var newValue = RandomProvider.Get(ctx.Seed).Next(0, 2) == 1;
        var hits = individual.Hits.ToList();
        hits.Insert(position, newValue);
        var lengths = individual.Lengths.ToList();
        lengths.Insert(position, 1);
        var velocity = individual.Velocity.ToList();
        velocity.Insert(position, (byte)RandomProvider.Get(ctx.Seed).Next(40, 80));
        individual.StepsTotal++;
        individual.Hits = hits.ToArray();
        individual.Lengths = lengths.ToArray();
        individual.Velocity = velocity.ToArray();
    }

    public RhythmPattern Crossover(RhythmPattern individual1, RhythmPattern individual2, EvolutionContext ctx)
    {
        var position = RandomProvider.Get(ctx.Seed).Next(0, Math.Min(individual1.Hits.Length, individual2.Hits.Length));
        var hits = individual1.Hits.Take(position).Concat(individual2.Hits.Skip(position)).ToArray();
        var lengths = individual1.Lengths.Take(position).Concat(individual2.Lengths.Skip(position)).ToArray();
        var velocity = individual1.Velocity.Take(position).Concat(individual2.Velocity.Skip(position)).ToArray();
        return new RhythmPattern(hits.Length)
        {
            Hits = hits,
            Lengths = lengths,
            Velocity = velocity,
        };
    }
}