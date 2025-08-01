namespace ChordProgressionGenerator.Models;

public class ChordSymbol
{
    public required string Symbol { get; set; }
    public required string RomanNumeral { get; set; }
    public required List<int> Notes { get; set; }

    public ChordSymbol(string symbol, string romanNumeral, List<int> notes)
    {
        Symbol = symbol;
        RomanNumeral = romanNumeral;
        Notes = notes;
    }

    public ChordSymbol() {}
}
