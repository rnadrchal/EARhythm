namespace StepMutator.Models.Evolution;

public interface IFitness
{
    double Weight { get; }
    double Evaluate(ulong individual) ;
}