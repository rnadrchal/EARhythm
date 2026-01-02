using System.ComponentModel.DataAnnotations;

namespace Egami.Sequencer.Grid;

public enum GridDivision
{
    [Display(Name = "32n")]
    ThirtySecond,
    [Display(Name = "16nt")]
    SixteenthTriplet,
    [Display(Name = "16n")]
    Sixteenth,    
    [Display(Name = "8nt")]
    EighthTriplet, 
    [Display(Name = "8n")]
    Eighth,  
    [Display(Name = "4nt")]
    QuarterTriplet,
    [Display(Name = "4n")]
    Quarter,  
    [Display(Name = "2nt")]
    HalfTriplet,
    [Display(Name = "2n")]
    Half,    
    [Display(Name = "1n")]
    Whole,      
    [Display(Name = "2 bars")]
    DoubleWhole   
}

public static class GridDivisionExtensions
{
    /// <summary>
    /// Anzahl MIDI-Clock-Pulse pro Grid-Schritt (24 PPQN).
    /// </summary>
    public static int GetPulsesPerStep(this GridDivision division)
    {
        return division switch
        {
            GridDivision.ThirtySecond => 3,
            GridDivision.SixteenthTriplet => 4,
            GridDivision.Sixteenth => 6,
            GridDivision.Eighth => 12,
            GridDivision.EighthTriplet => 8,
            GridDivision.Quarter => 24,
            GridDivision.QuarterTriplet => 16,
            GridDivision.Half => 48,
            GridDivision.HalfTriplet => 32,
            GridDivision.Whole => 96,
            GridDivision.DoubleWhole => 192,
            _ => 24
        };
    }
}