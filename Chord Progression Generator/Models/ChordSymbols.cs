namespace ChordProgressionGenerator.Models;

public class ChordSymbol
{
    public required string Symbol { get; set; }
    public required string RomanNumeral { get; set; }
    public required List<string> Notes { get; set; }
    public List<string>? Synonyms { get; set; }

    public ChordSymbol(string symbol, string romanNumeral, List<string> notes, List<string>? synonyms = null)
    {
        Symbol = symbol;
        RomanNumeral = romanNumeral;
        Notes = notes;
        Synonyms = synonyms;
    }

    public ChordSymbol() {}

    // Returns all names associated with this chord: Symbol, RomanNumeral, and any defined synonyms
    public List<string> AllNames()
    {
        List<string> names = new() { Symbol, RomanNumeral };

        if (Synonyms != null)
            names.AddRange(Synonyms.Where(s => !string.IsNullOrWhiteSpace(s)));
        
        return names.Distinct().ToList();
    }
}
