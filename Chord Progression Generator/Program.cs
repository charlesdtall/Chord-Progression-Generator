using System;
using ChordProgressionGenerator.Models;
using ChordProgressionGenerator.Services;

namespace ChordProgressionGenerator;

class Program
{

    static void Main()
    {
        var chordService = new ChordSymbolService("data/chordSymbols.json");
        List<ChordSymbol> chords = chordService.LoadChords();
        var progressionService = new ChordProgressionService("data/chordProgressions.json");
        List<ChordProgression> progressions = progressionService.LoadProgressions();

        ListChordFrequencies(chords, progressions);
        //ListProgressions(progressions);
        //ListChords(chordSymbols);
    }

    public static Dictionary<ChordPair, int> GetChordPairFrequencies(
        List<ChordSymbol> chords, List<ChordProgression> progressions)
    {
        Dictionary<string, string> chordCanonicalLookup = BuildCanonicalLookup(chords);
        Dictionary<ChordPair, int> pairCounts = CountChordPairs(progressions, chordCanonicalLookup);
        return pairCounts;
    }

    static Dictionary<string, string> BuildCanonicalLookup(List<ChordSymbol> chords)
    {
        return chords
            .SelectMany(c => new[] { c.RomanNumeral, c.Symbol })
            .Where(k => !string.IsNullOrEmpty(k))
            .Distinct()
            .ToDictionary(
                k => k,
                k => chords.First(c => c.RomanNumeral == k || c.Symbol == k).RomanNumeral
            );
    }


    public static List<string> FlattenAndNormalize(ChordProgression progression, Dictionary<string, string> canonicalLookup)
    {
        var flat = new List<string>();
        foreach (var bar in progression.Bars)
        {
            foreach (var beat in bar)
            {
                foreach (var chord in beat)
                {
                    if (canonicalLookup.TryGetValue(chord, out var normalized))
                        flat.Add(normalized);
                    else
                        flat.Add(chord);
                }
            }
        }
        return flat;
    }

    public static Dictionary<ChordPair, int> CountChordPairs(
        List<ChordProgression> progressions, Dictionary<string, string> canonicalLookup)
    {
        Dictionary<ChordPair, int> pairCounts = new();

        foreach (ChordProgression progression in progressions)
        {
            // Flatten and normalize the entire progression
            List<string> flatChords = FlattenAndNormalize(progression, canonicalLookup);

            for (int i = 0; i < flatChords.Count - 1; i++)
            {
                string from = flatChords[i];
                string to = flatChords[i + 1];
                ChordPair pair = new(from, to);

                if (pairCounts.ContainsKey(pair))
                    pairCounts[pair]++;
                else
                    pairCounts[pair] = 1;
            }

            if (progression.Type == "Loop" && flatChords.Count > 1)
            {
                string from = flatChords[^1];
                string to = flatChords[0];
                ChordPair loopPair = new(from, to);

                if (pairCounts.ContainsKey(loopPair))
                    pairCounts[loopPair]++;
                else
                    pairCounts[loopPair] = 1;
            }
        }

        return pairCounts;
    }


    static void ListChords(List<ChordSymbol> chords)
    {
        foreach (var chord in chords)
        {
            Console.WriteLine($"{chord.Symbol} ({chord.RomanNumeral}): {string.Join(", ", chord.Notes)}");
        }
    }

    static void ListProgressions(List<ChordProgression> progressions)
    {
        foreach (var prog in progressions)
        {
            Console.WriteLine("=============================");
            Console.WriteLine($"Name: {prog.Name ?? "Untitled"}");

            if (prog.Year.HasValue)
                Console.WriteLine($"Year: {prog.Year?.ToString() ?? "Unknown"}");
            
            if (!string.IsNullOrWhiteSpace(prog.Period))
                Console.WriteLine($"Period: {prog.Period ?? "Unknown"}");

            if (!string.IsNullOrWhiteSpace(prog.Genre))
                Console.WriteLine($"Genre: {prog.Genre ?? "Unknown"}");
            
            if (!string.IsNullOrWhiteSpace(prog.Type))
                Console.Write($"Type: {prog.Type ?? "Unknown"}");

            Console.WriteLine($"Progression:");
            for (int barIndex = 0; barIndex < prog.Bars.Count; barIndex++)
            {
                var bar = prog.Bars[barIndex];
                Console.Write($"Bar {barIndex + 1}: ");

                var timeDivisions = bar
                    .Select(timeUnit => string.Join(" ", timeUnit))
                    .ToList();
                
                Console.WriteLine(string.Join(" | ", timeDivisions));
            }

            Console.WriteLine();
        }
    }

    static void ListChordFrequencies(List<ChordSymbol> chords, List<ChordProgression> progressions)
    {
        Dictionary<ChordPair, int> pairCounts = GetChordPairFrequencies(chords, progressions);

        foreach (var kvp in pairCounts.OrderByDescending(kvp => kvp.Value))
        {
            Console.WriteLine($"{kvp.Key.From} -> {kvp.Key.To} : {kvp.Value} times");
        }
    }

}