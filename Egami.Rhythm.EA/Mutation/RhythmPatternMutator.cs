using Egami.Rhythm.Common;
using Egami.Rhythm.Pattern;

namespace Egami.Rhythm.EA.Mutation;

public class RhythmPatternMutator : IMutator<Sequence>
{
    public void Mutate(Sequence individual, IEvolutionOptions options)
    {
        var position = RandomProvider.Get(options.Seed).Next(0, individual.Hits.Length);
        var d = RandomProvider.Get(options.Seed).NextDouble();
        if (d <= 0.25)
        {
            individual.Hits[position] = !individual.Hits[position];
        }
        else if (d <= 0.5)
        {
            individual.Steps[position].Pitch = RandomProvider.Get(options.Seed).Next(21, 108);
        }
        else if (d <= 0.75)
        {
            individual.Steps[position].Pitch = (byte)RandomProvider.Get(options.Seed).Next(5, 127);
        }
        else
        {
            var stepLength = RandomProvider.Get(options.Seed).Next(1, options.MaxStepLength);
            individual.Steps[position].Length = stepLength;
        }
    }

    public void Delete(Sequence individual, IEvolutionOptions options)
    {
        var position = RandomProvider.Get(options.Seed).Next(0, individual.Hits.Length);
        individual.Steps.RemoveAt(position);
        individual.StepsTotal--;
    }

    public void Insert(Sequence individual, IEvolutionOptions options)
    {
        var position = RandomProvider.Get(options.Seed).Next(0, individual.Hits.Length);
        individual.Steps.Insert(position, new Step
        {
            Hit = RandomProvider.Get(options.Seed).Next(0, 2) == 1,
            Pitch = RandomProvider.Get(options.Seed).Next(21, 108),
            Velocity = RandomProvider.Get(options.Seed).Next(5, 127),
            Length = RandomProvider.Get(options.Seed).Next(1, options.MaxStepLength)
        });
        individual.StepsTotal++;
    }

    public void Swap(Sequence individual, IEvolutionOptions options)
    {
        var pos1 = RandomProvider.Get(options.Seed).Next(0, individual.Hits.Length);    
        var pos2 = RandomProvider.Get(options.Seed).Next(0, individual.Hits.Length);
        if (pos1 != pos2)
        {
            (individual.Steps[pos1], individual.Steps[pos2]) = (individual.Steps[pos2], individual.Steps[pos1]);
        }
    }

    public Sequence Crossover(Sequence individual1, Sequence individual2, IEvolutionOptions options)
    {
        var position = RandomProvider.Get(options.Seed).Next(0, Math.Min(individual1.Hits.Length, individual2.Hits.Length));
        var steps = individual1.Steps.Take(position).Concat(individual2.Steps.Skip(position)).ToList();
        return new Sequence(steps.Count)
        {
            Steps = steps
        };
    }
}