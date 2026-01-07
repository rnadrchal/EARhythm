using System.ComponentModel.DataAnnotations;

namespace Egami.Sequencer.Common;

public enum ClockDivider
{
    [Display(Name = "32n")]
    ThirtySecond = 3,
    [Display(Name = "16nt")]
    ThirtySecondTriolic = 4,
    [Display(Name = "16n")]
    Sixteenth = 6,
    [Display(Name = "8nt")]
    EighthTriolic = 8,
    [Display(Name = "8n")]
    Eighth = 12,
    [Display(Name = "4nt")]
    QuarterTriolic = 16,
    [Display(Name = "4n")]
    Quarter = 24,
    [Display(Name = "2nt")]
    HalfTriolic = 32,
    [Display(Name = "2n")]
    Half = 48,
    [Display(Name = "1n")]
    Whole = 96,
    [Display(Name = "2 * 1n")]
    DoubleWhole = 192,
    [Display(Name = "4 * 1n")]
    QuatrupleWhole = 384
}