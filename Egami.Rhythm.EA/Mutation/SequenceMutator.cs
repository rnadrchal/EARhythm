using Egami.Rhythm.Common;
using Egami.Rhythm.Pattern;
using Microsoft.VisualBasic;

namespace Egami.Rhythm.EA.Mutation;

public class SequenceMutator : IMutator<Sequence>
{
    public void Mutate(Sequence individual, IEvolutionOptions options)
    {
        var position = RandomProvider.Get(options.Seed).Next(0, individual.Hits.Length);
        var d = RandomProvider.Get(options.Seed).Next(0, 19 * options.PopulationSize);
        if (d < 1)
        {
            individual.Hits[position] = !individual.Hits[position];
        }
        else if (d < 8)
        {
            individual.Steps[position].Pitch = RandomProvider.Get(options.Seed).Next(21, 108);
        }
        else if (d < 15)
        {
            individual.Steps[position].Velocity = (byte)RandomProvider.Get(options.Seed).Next(5, 127);
        }
        else if (d < 19 && options.MaxStepLength > 1)
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

    public void Inversion(Sequence individual, IEvolutionOptions options)
    {
        var p1 = RandomProvider.Get(options.Seed).Next(0, individual.StepsTotal);

        var p2 = p1 > individual.StepsTotal / 2
            ? RandomProvider.Get(options.Seed).Next(0, p1)
            : RandomProvider.Get(options.Seed).Next(p1, individual.StepsTotal);
        var start = Math.Min(p1, p2);
        var end = Math.Max(p1, p2);
        var segment = individual.Steps.GetRange(start, end - start + 1);
        segment.Reverse();
        for (var i = start; i <= end; i++)
        {
            individual.Steps[i] = segment[i - start];
        }
    }

    public void Transposition(Sequence individual, IEvolutionOptions options)
    {
        var p1 = RandomProvider.Get(options.Seed).Next(0, individual.StepsTotal);
        var p2 = p1 > individual.StepsTotal / 2
            ? RandomProvider.Get(options.Seed).Next(0, p1)
            : RandomProvider.Get(options.Seed).Next(p1, individual.StepsTotal);
        var start = Math.Min(p1, p2);
        var end = Math.Max(p1, p2);
        var segment = individual.Steps.GetRange(start, end - start + 1);
        individual.Steps.RemoveRange(start, end - start + 1);
        var insertPos = RandomProvider.Get(options.Seed).Next(0, individual.Steps.Count + 1);
        individual.Steps.InsertRange(insertPos, segment);
    }

    public void Retrograde(Sequence individual, IEvolutionOptions options)
    {
        individual.Steps.Reverse();
    }

    public void MelodicInversion(Sequence individual, IEvolutionOptions options)
    {
        if (individual.Steps.Count == 0) return;
        var firstPitch = individual.Steps[0].Pitch;
        for (var i = 1; i < individual.Steps.Count; i++)
        {
            var interval = individual.Steps[i].Pitch - individual.Steps[i - 1].Pitch;
            individual.Steps[i].Pitch = individual.Steps[i - 1].Pitch - interval;
            if (individual.Steps[i].Pitch < 21) individual.Steps[i].Pitch = 21;
            if (individual.Steps[i].Pitch > 108) individual.Steps[i].Pitch = 108;
        }
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