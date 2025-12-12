namespace Egami.Sequencer.Grid;

public enum GridDivision
{
    ThirtySecond,
    SixteenthTriplet,
    Sixteenth,      
    Eighth,        
    EighthTriplet, 
    Quarter,      
    QuarterTriplet,
    Half,          
    HalfTriplet,
    Whole,         
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