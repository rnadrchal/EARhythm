using Egami.Rhythm.EA.Mutation;

namespace Egami.Rhythm.EA;

public class EvolutionContext
{
    public required double MutationRate { get; set; } = 0.1;
    public required double DeletionRate { get; set; } = 0.01;
    public required double InsertionRate { get; set; } = 0.01;
    public required double SwapRate { get; set; } = 0.1;
    public required double CrossoverRate { get; set; } = 0.7;
    public required double RhythmWeight { get; set; } = 1.0;
    public required double PitchWeight { get; set; } = 1.0;
    public required double LenghtWeight { get; set; } = 1.0;
    public required double VelocityWeight { get; set; } = 1.0;
    public int? Seed { get; init; } = null;

    public static EvolutionContext Create(double mutationRate = 0.1, double deletionRate = 0.01,
        double insertionRate = 0.01, double swapRate = 0.1, double crossoverRate = 0.7,
        double rhythmWeight = 1.0, double pitchWeight = 1.0, double lengthWeight = 1.0, double velocityWeight = 1.0)
    {
        return new EvolutionContext
        {
            MutationRate = mutationRate,
            DeletionRate = deletionRate,
            InsertionRate = insertionRate,
            CrossoverRate = crossoverRate,
            SwapRate = swapRate,
            RhythmWeight = rhythmWeight,
            PitchWeight = pitchWeight,
            LenghtWeight = lengthWeight,
            VelocityWeight = velocityWeight
        };
    }
}