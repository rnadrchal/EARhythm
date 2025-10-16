using Egami.Rhythm.Common;
using Egami.Rhythm.Pattern;

namespace Egami.Rhythm.EA.Mutation;

public class RhythmPatternMutator : IMutator<RhythmPattern>
{
    public void Mutate(RhythmPattern individual, IEvolutionOptions options)
    {
        var position = RandomProvider.Get(options.Seed).Next(0, individual.Hits.Length);
        var d = RandomProvider.Get(options.Seed).NextDouble();
        if (d <= 0.3)
        {
            individual.Hits[position] = !individual.Hits[position];
        }
        else if (d <= 0.6)
        {
            if (individual.Hits[position])
            {
                individual.Pitches[position] = RandomProvider.Get(options.Seed).Next(21, 108);
            }
        }
        else if (d <= 0.9)
        {
            individual.Velocities[position] = (byte)RandomProvider.Get(options.Seed).Next(5, 127);
        }
        else
        {
            if (RandomProvider.Get(options.Seed).NextDouble() <= 0.5)
            {
                individual.Lengths[position] = RandomProvider.Get(options.Seed).Next(individual.Lengths[position] + 1, 17);
            }
            else
            {
                if (individual.Lengths[position] > 1)
                {
                    individual.Lengths[position] = RandomProvider.Get(options.Seed).Next(1, individual.Lengths[position]);
                }
                else
                {
                    individual.Lengths[position] = 1; // Assign a default value or handle appropriately
                }
            }
        }
    }

    public void Delete(RhythmPattern individual, IEvolutionOptions options)
    {
        var position = RandomProvider.Get(options.Seed).Next(0, individual.Hits.Length);
        var hits = individual.Hits.ToList();
        hits.RemoveAt(position);
        var lengths = individual.Lengths.ToList();
        lengths.RemoveAt(position);
        var velocity = individual.Velocities.ToList();
        velocity.RemoveAt(position);
        var pitches = individual.Pitches.ToList();
        pitches.RemoveAt(position);
        individual.StepsTotal--;
        individual.Hits = hits.ToArray();
        individual.Lengths = lengths.ToArray();
        individual.Velocities = velocity.ToArray();
        individual.Pitches = pitches.ToArray();
    }

    public void Insert(RhythmPattern individual, IEvolutionOptions options)
    {
        var position = RandomProvider.Get(options.Seed).Next(0, individual.Hits.Length);
        var newValue = RandomProvider.Get(options.Seed).Next(0, 2) == 1;
        var hits = individual.Hits.ToList();
        hits.Insert(position, newValue);
        var lengths = individual.Lengths.ToList();
        lengths.Insert(position, 1);
        var velocity = individual.Velocities.ToList();
        velocity.Insert(position, (byte)RandomProvider.Get(options.Seed).Next(40, 80));
        var minPitch = individual.Pitches.Where(p => p.HasValue).DefaultIfEmpty(60).Min();
        var maxPitch = individual.Pitches.Where(p => p.HasValue).DefaultIfEmpty(72).Max();
        if (minPitch == maxPitch) maxPitch = minPitch + 12;
        var pitches = individual.Pitches.ToList();
        pitches.Insert(position, (byte)RandomProvider.Get(options.Seed).Next(minPitch ?? 60, maxPitch ?? 72));
        individual.StepsTotal++;
        individual.Hits = hits.ToArray();
        individual.Lengths = lengths.ToArray();
        individual.Velocities = velocity.ToArray();
        individual.Pitches = pitches.ToArray();
    }

    public void Swap(RhythmPattern individual, IEvolutionOptions options)
    {
        var pos1 = RandomProvider.Get(options.Seed).Next(0, individual.Hits.Length);    
        var pos2 = RandomProvider.Get(options.Seed).Next(0, individual.Hits.Length);
        if (pos1 != pos2)
        {
            var hit = individual.Hits[pos1];
            var pitch = individual.Pitches[pos1];
            var length = individual.Lengths[pos1];
            var velocity = individual.Velocities[pos1];
            individual.Hits[pos1] = individual.Hits[pos2];
            individual.Pitches[pos1] = individual.Pitches[pos2];
            individual.Lengths[pos1] = individual.Lengths[pos2];
            individual.Velocities[pos1] = individual.Velocities[pos2];
            individual.Hits[pos2] = hit;
            individual.Pitches[pos2] = pitch;
            individual.Lengths[pos2] = length;
            individual.Velocities[pos2] = velocity;
        }
    }

    public RhythmPattern Crossover(RhythmPattern individual1, RhythmPattern individual2, IEvolutionOptions options)
    {
        var position = RandomProvider.Get(options.Seed).Next(0, Math.Min(individual1.Hits.Length, individual2.Hits.Length));
        var hits = individual1.Hits.Take(position).Concat(individual2.Hits.Skip(position)).ToArray();
        var lengths = individual1.Lengths.Take(position).Concat(individual2.Lengths.Skip(position)).ToArray();
        var velocity = individual1.Velocities.Take(position).Concat(individual2.Velocities.Skip(position)).ToArray();
        var pitches = individual1.Pitches.Take(position).Concat(individual2.Pitches.Skip(position)).ToArray();
        return new RhythmPattern(hits.Length)
        {
            Hits = hits,
            Lengths = lengths,
            Velocities = velocity,
            Pitches = pitches
        };
    }
}