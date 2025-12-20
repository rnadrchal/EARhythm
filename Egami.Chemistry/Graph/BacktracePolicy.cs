using System.ComponentModel.DataAnnotations;

namespace Egami.Chemistry.Graph;

public enum BacktracePolicy
{
    [Display(Name = "Always")]
    Always,                 // klassischer DFS: jeder Rückweg wird gespielt
    [Display(Name = "Never")]
    Never,                  // nie Rückwege spielen (Teleport)
    [Display(Name = "Cycles")]
    OnlyInCycles,           // Rückwege nur, wenn der Node in einem Ring/Zyklus liegt
    [Display(Name = "Branching")]
    OnlyWhenBranching,      // Rückwege nur, wenn der Node/Parent eine Verzweigung hat
    [Display(Name = "Depth")]
    OnlyIfDepthAtLeast      // Rückwege nur ab einer Mindesttiefe
}