using System.Collections.Generic;

namespace StepMutator.Models;

public interface ISequence
{
    int Length { get; set; }
    IEnumerable<IStep> Steps { get; }
}