using System.ComponentModel.DataAnnotations;

namespace Blattwerk.ViewModels;

public enum PitchType
{
    [Display(Name = "Percussion")]
    Percussion,
    [Display(Name = "Drum Kit")]
    DrumKit,
    [Display(Name = "Piano")]
    Piano,
    Vocals,
    Strings,
    Reeds
}