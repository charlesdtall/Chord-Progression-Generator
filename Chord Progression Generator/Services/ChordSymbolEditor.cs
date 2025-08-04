using System.Text.Json;
using ChordProgressionGenerator.Models;
using ChordProgressionGenerator.Services;

namespace ChordProgressionGenerator.Utils;

public class ChordSymbolEditor
{
    private readonly ChordSymbolService _service;

    public ChordSymbolEditor(ChordSymbolService service)
    {
        _service = service;
    }

    public void PromptToAddMissingChords(List<string> chordInputs)
    {
        List<ChordSymbol> existingChords = _service.LoadChords();
        HashSet<string> knownSymbols = existingChords
            .SelectMany(c => new[] { c.Symbol, c.RomanNumeral }.Concat(c.Synonyms ?? new List<string>()))
            .ToHashSet();
        List<string> unknownChords = chordInputs
            .Where(c => !knownSymbols.Contains(c))
            .Distinct()
            .ToList();

        if (unknownChords.Count == 0)
            return;

        Console.WriteLine("There are unrecognized chords in this progression:");
        foreach (var chord in unknownChords)
            Console.WriteLine($"- {chord}");

        Console.Write("Do you want to add them to the database? (y/n): ");
        string? input = Console.ReadLine();
        if (input == null || !input.Trim().Equals("y", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("No chords were added.");
            return;
        }

        List<ChordSymbol> newChords = new();

        foreach (string chord in unknownChords)
        {
            Console.WriteLine($"\nIs \"{chord}\" a chord symbol or Roman numeral? (symbol/numeral): ");
            string? typeInput = Console.ReadLine();
            string type = typeInput?.Trim().ToLower() ?? "";

            string symbol = "";
            string roman = "";
            List<string> synonyms = new();

            if (type == "symbol")
            {
                Console.Write($"Is \"{chord}\" the best way to spell this chord? (y/n): ");
                string? bestInput = Console.ReadLine();
                string best = bestInput?.Trim().ToLower() ?? "";

                if (best == "y")
                {
                    symbol = chord;
                }
                else
                {
                    synonyms.Add(chord);
                    Console.Write("What is the best way to spell it? ");
                    string? bestSpellingInput = Console.ReadLine();
                    symbol = bestSpellingInput?.Trim() ?? "";
                }

                Console.Write("Enter the Roman numeral: ");
                string? romanInput = Console.ReadLine();
                roman = romanInput?.Trim() ?? "";
            }
            else // Roman numeral input
            {
                Console.Write($"Is \"{chord}\" the best Roman numeral spelling? (y/n): ");
                string? bestInput = Console.ReadLine();
                string best = bestInput?.Trim().ToLower() ?? "";

                if (best == "y")
                {
                    roman = chord;
                }
                else
                {
                    synonyms.Add(chord);
                    Console.Write("What is the best way to spell it? ");
                    string? bestRomanInput = Console.ReadLine();
                    roman = bestRomanInput?.Trim() ?? "";
                }

                Console.Write("What would be the best chord symbol for this? (key of C): ");
                string? symbolInput = Console.ReadLine();
                symbol = symbolInput?.Trim() ?? "";
            }

            Console.Write("What notes are in this chord? (comma separated, bass note first): ");
            string? notesInput = Console.ReadLine();
            List<string> notes = notesInput?
                .Split(',')
                .Select(n => n.Trim())
                .Where(n => n.Length > 0)
                .ToList() ?? new();

            // Check if a chord with this Symbol and RomanNumeral already exists
            ChordSymbol? existingMatch = existingChords.FirstOrDefault(c =>
                string.Equals(c.Symbol, symbol, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(c.RomanNumeral, roman, StringComparison.OrdinalIgnoreCase));

            if (existingMatch != null)
            {
                existingMatch.Synonyms ??= new List<string>();

                foreach (string synonym in synonyms)
                {
                    if (!existingMatch.Synonyms.Contains(synonym, StringComparer.OrdinalIgnoreCase))
                    {
                        existingMatch.Synonyms.Add(synonym);
                    }
                }

                if (existingMatch.Notes == null || existingMatch.Notes.Count == 0)
                {
                    existingMatch.Notes = notes;
                }

                Console.WriteLine($"Merged into existing chord: {existingMatch.Symbol} / {existingMatch.RomanNumeral}");
            }
            else
            {
                ChordSymbol newChord = new()
                {
                    Symbol = symbol,
                    RomanNumeral = roman,
                    Synonyms = synonyms,
                    Notes = notes
                };

                newChords.Add(newChord);
            }
        }

        Console.WriteLine("\nHere are the new chords:");
        for (int i = 0; i < newChords.Count; i++)
        {
            var chord = newChords[i];
            Console.WriteLine($"{i + 1}. Symbol: {chord.Symbol}, Roman Numeral: {chord.RomanNumeral}, Notes: [{string.Join(", ", chord.Notes)}], Synonyms: [{string.Join(", ", chord.Synonyms)}]");
        }

        Console.Write("Do those look good? (y/n): ");
        string? confirmInput = Console.ReadLine();
        if (confirmInput == null || !confirmInput.Trim().Equals("y", StringComparison.OrdinalIgnoreCase))
        {
            Console.Write("Which chord(s) do you want to fix? (comma separated indices starting at 1): ");
            string? fixInput = Console.ReadLine();
            List<int> indices = fixInput?
                .Split(',')
                .Select(s => int.TryParse(s.Trim(), out int i) ? i - 1 : -1)
                .Where(i => i >= 0 && i < newChords.Count)
                .Distinct()
                .OrderByDescending(i => i)
                .ToList() ?? new();

            foreach (int index in indices)
            {
                var chord = newChords[index];
                Console.WriteLine($"\nRe-entering info for: {chord.Symbol}");

                PromptToAddMissingChords(new List<string> { chord.Symbol });
                newChords.RemoveAt(index);
            }
        }

        existingChords.AddRange(newChords);

        var sortedChords = existingChords
            .OrderBy(c => GetSortValue(c.Symbol))
            .ThenBy(c => c.Symbol)
            .ToList();

        _service.SaveChords(sortedChords);
        Console.WriteLine("Thank you! Chords saved.");
    }

    private static int GetSortValue(string symbol)
    {
        string root = new string(symbol.TakeWhile(c => char.IsLetter(c) || c == '#').ToArray());

        return root switch
        {
            "C"  => 1,
            "C#" => 2,
            "Db" => 3,
            "D"  => 4,
            "D#" => 5,
            "Eb" => 6,
            "E"  => 7,
            "F"  => 8,
            "F#" => 9,
            "Gb" => 10,
            "G"  => 11,
            "G#" => 12,
            "Ab" => 13,
            "A"  => 14,
            "A#" => 15,
            "Bb" => 16,
            "B"  => 17,
            _    => 99
        };
    }
}
