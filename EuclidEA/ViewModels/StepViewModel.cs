namespace EuclidEA.ViewModels;

public class StepViewModel
{
    private static string[] _noteNames = ["C", "C#", "D", "Eb", "E", "F", "F#", "G", "Ab", "A", "Bb", "B"];
    public bool IsHit { get; set; }
    public int Length { get; set; }
    public int Velocity { get; set; }
    public int? Pitch { get; set; }
    public string NoteName => Pitch.HasValue ? $"{_noteNames[Pitch.Value % 12]}{Pitch.Value / 12}" : string.Empty;

}