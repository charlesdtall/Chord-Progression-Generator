namespace ChordProgressionGenerator.Models;

public class ChordProgression
{
    public string? Name { get; set; }
    public int? Year { get; set; }
    public string? Period { get; set; }
    public string? Genre { get; set; }
    public string? Type { get; set; }
    public List<List<List<string>>> Bars { get; set; } = new();
}