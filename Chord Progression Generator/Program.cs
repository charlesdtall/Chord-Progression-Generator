using System;
using System.Collections.Generic;
using System.Linq;
using ChordProgressionGenerator.Models;
using ChordProgressionGenerator.Services;
using ChordProgressionGenerator.Utils

namespace ChordProgressionGenerator
{
    class Program
    {
        static void Main()
        {
            // Load chord definitions from JSON file
            ChordSymbolService chordService = new ChordSymbolService("data/chordSymbols.json");
            List<ChordSymbol> chords = chordService.LoadChords();

            // Load chord progressions from JSON file
            ChordProgressionService progressionService = new ChordProgressionService("data/chordProgressions.json");
            List<ChordProgression> progressions = progressionService.LoadProgressions();

            ListFilteredProgressions(progressions);
            //ListFilteredChordFrequencies(chords, progressions);
            //ListChordFrequencies(chords, progressions);
            //ListProgressions(progressions);
            //ListChords(chords);
        }

        // Computes the frequency (as a percentage) of chord transitions across all progressions
        public static Dictionary<ChordPair, double> GetChordPairFrequencies(
            List<ChordSymbol> chords, List<ChordProgression> progressions)
        {
            Dictionary<string, string> chordCanonicalLookup = BuildCanonicalLookup(chords);
            Dictionary<ChordPair, int> rawCounts = CountChordPairs(progressions, chordCanonicalLookup);

            int totalPairs = rawCounts.Values.Sum();

            Dictionary<ChordPair, double> percentages = rawCounts.ToDictionary(
                kvp => kvp.Key,
                kvp => (double)kvp.Value / totalPairs
            );
            return percentages;
        }

        // Builds a lookup table mapping each chord symbol to its canonical Roman numeral form
        static Dictionary<string, string> BuildCanonicalLookup(List<ChordSymbol> chords)
        {
            Dictionary<string, string> lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (ChordSymbol chord in chords)
            {
                foreach (string name in chord.AllNames())
                {
                    if(!lookup.ContainsKey(name))
                    {
                        lookup[name] = chord.RomanNumeral;
                    }
                }
            }

            return lookup;
        }

        // Flattens a nested chord progression into a single list of chord strings, normalized to Roman numerals
        public static List<string> FlattenAndNormalize(ChordProgression progression, Dictionary<string, string> canonicalLookup)
        {
            List<string> flat = new List<string>();

            foreach (List<List<string>> bar in progression.Bars)
            {
                foreach (List<string> beat in bar)
                {
                    foreach (string chord in beat)
                    {
                        if (canonicalLookup.TryGetValue(chord, out string? normalized))
                            flat.Add(normalized);
                        else
                            flat.Add(chord);
                    }
                }
            }

            return flat;
        }

        // Counts how many times each chord pair appears across all progressions
        public static Dictionary<ChordPair, int> CountChordPairs(
            List<ChordProgression> progressions, Dictionary<string, string> canonicalLookup)
        {
            Dictionary<ChordPair, int> pairCounts = new Dictionary<ChordPair, int>();

            foreach (ChordProgression progression in progressions)
            {
                // Flatten and normalize the entire progression
                List<string> flatChords = FlattenAndNormalize(progression, canonicalLookup);

                for (int i = 0; i < flatChords.Count - 1; i++)
                {
                    string from = flatChords[i];
                    string to = flatChords[i + 1];
                    ChordPair pair = new ChordPair(from, to);

                    if (pairCounts.ContainsKey(pair))
                        pairCounts[pair]++;
                    else
                        pairCounts[pair] = 1;
                }

                // Handle looping progressions by connecting last to first chord
                if (progression.Type == "Loop" && flatChords.Count > 1)
                {
                    string from = flatChords[^1];
                    string to = flatChords[0];
                    ChordPair loopPair = new ChordPair(from, to);

                    if (pairCounts.ContainsKey(loopPair))
                        pairCounts[loopPair]++;
                    else
                        pairCounts[loopPair] = 1;
                }
            }

            return pairCounts;
        }

        // Takes list of progressions and filters it by optional criteria
        public static List<ChordProgression> FilterProgressions(
            List<ChordProgression> progressions,
            string? genre,
            string? period,
            string? composer,
            string? artist,
            string? type,
            int? yearAfter,
            int? yearBefore)
        {
            return progressions
                .Where(p =>
                    (string.IsNullOrEmpty(genre) || p.Genre?.Equals(genre, StringComparison.OrdinalIgnoreCase) == true) &&
                    (string.IsNullOrEmpty(period) || p.Period?.Equals(period, StringComparison.OrdinalIgnoreCase) == true) &&
                    (string.IsNullOrEmpty(composer) || p.Composer?.Equals(composer, StringComparison.OrdinalIgnoreCase) == true) &&
                    (string.IsNullOrEmpty(artist) || p.Artist?.Equals(artist, StringComparison.OrdinalIgnoreCase) == true) &&
                    (string.IsNullOrEmpty(type) || p.Type?.Equals(type, StringComparison.OrdinalIgnoreCase) == true) &&
                    (!yearAfter.HasValue || (p.Year.HasValue && p.Year.Value >= yearAfter.Value)) &&
                    (!yearBefore.HasValue || (p.Year.HasValue && p.Year.Value <= yearBefore.Value))
                )
                .ToList();
        }

        // Returns frequency of pairs once they are filtered
        public static Dictionary<ChordPair, double> GetFilteredChordPairFrequencies(
            List<ChordSymbol> chords,
            List<ChordProgression> allProgressions,
            string? genre,
            string? period,
            string? artist,
            string? composer,
            string? type,
            int? yearAfter,
            int? yearBefore)
        {
            // First, filter progressions using optional metadata criteria
            List<ChordProgression> filtered = FilterProgressions(allProgressions, genre, period, artist, composer, type, yearAfter, yearBefore);

            // Then compute chord pair frequencies on the filtered list
            return GetChordPairFrequencies(chords, filtered);
        }

        // Turns Filtered Pair Frequencies into a command line prompt for user interaction
        public static Dictionary<ChordPair, double> GetChordPairFrequenciesWithOptionalFiltering(
            List<ChordSymbol> chords, List<ChordProgression> progressions)
        {
            Console.Write("Do you want to apply filters? (y/n): ");
            string? response = Console.ReadLine()?.Trim().ToLower();

            if (response != "y")
            {
                return GetChordPairFrequencies(chords, progressions);
            }

            Console.Write("Filter by Genre (or leave blank): ");
            string? genre = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(genre)) genre = null;

            Console.Write("Filter by Period (or leave blank): ");
            string? period = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(period)) period = null;

            Console.Write("Filter by Artist (or leave blank): ");
            string? artist = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(artist)) artist = null;

            Console.Write("Filter by Composer (or leave blank): ");
            string? composer = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(composer)) composer = null;

            Console.Write("Filter by Type (or leave blank): ");
            string? type = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(type)) type = null;

            Console.Write("Filter by minimum Year (or leave blank): ");
            string? yearAfterInput = Console.ReadLine()?.Trim();
            int? yearAfter = int.TryParse(yearAfterInput, out int ya) ? ya : null;

            Console.Write("Filter by maximum Year (or leave blank): ");
            string? yearBeforeInput = Console.ReadLine()?.Trim();
            int? yearBefore = int.TryParse(yearBeforeInput, out int yb) ? yb : null;

            return GetFilteredChordPairFrequencies(chords, progressions, genre, period, artist, composer, type, yearAfter, yearBefore);
        }

/*============================TESTING MODULES===============================*/

        // Prints out each chord with its Roman numeral and note list
        static void ListChords(List<ChordSymbol> chords)
        {
            foreach (ChordSymbol chord in chords)
            {
                string synonyms = (chord.Synonyms != null && chord.Synonyms.Any())
                    ? $", {string.Join(", ", chord.Synonyms)}"
                    : "";
                
                Console.WriteLine($"{chord.Symbol} ({chord.RomanNumeral}{synonyms}): {string.Join(", ", chord.Notes)}");
            }
        }

        // Prints out all progressions with their metadata and structure
        static void ListProgressions(List<ChordProgression> progressions)
        {
            foreach (ChordProgression prog in progressions)
            {
                Console.WriteLine("=============================");
                Console.WriteLine($"Name: {prog.Name ?? "Untitled"}");

                if (prog.Year.HasValue)
                    Console.WriteLine($"Year: {prog.Year?.ToString() ?? "Unknown"}");

                if (!string.IsNullOrWhiteSpace(prog.Period))
                    Console.WriteLine($"Period: {prog.Period ?? "Unknown"}");

                if (!string.IsNullOrWhiteSpace(prog.Artist))
                    Console.WriteLine($"Artist: {prog.Artist ?? "Unknown"}");

                if (!string.IsNullOrWhiteSpace(prog.Composer))
                    Console.WriteLine($"Composer: {prog.Composer ?? "Unknown"}");

                if (!string.IsNullOrWhiteSpace(prog.Genre))
                    Console.WriteLine($"Genre: {prog.Genre ?? "Unknown"}");

                if (!string.IsNullOrWhiteSpace(prog.Type))
                    Console.Write($"Type: {prog.Type ?? "Unknown"}");

                Console.WriteLine($"Progression:");

                for (int barIndex = 0; barIndex < prog.Bars.Count; barIndex++)
                {
                    List<List<string>> bar = prog.Bars[barIndex];
                    Console.Write($"Bar {barIndex + 1}: ");

                    List<string> timeDivisions = bar
                        .Select(timeUnit => string.Join(" ", timeUnit))
                        .ToList();

                    Console.WriteLine(string.Join(" | ", timeDivisions));
                }

                Console.WriteLine();
            }
        }

        static void ListFilteredProgressions(List<ChordProgression> allProgressions)
        {
            Console.Write("Do you want to apply filters? (y/n): ");
            string? response = Console.ReadLine()?.Trim().ToLower();

            string? genre = null;
            string? period = null;
            string? artist = null;
            string? composer = null;
            string? type = null;
            int? yearAfter = null;
            int? yearBefore = null;

            if (response == "y")
            {
                Console.Write("Filter by Genre (or leave blank): ");
                genre = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(genre)) genre = null;

                Console.Write("Filter by Period (or leave blank): ");
                period = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(period)) period = null;

                Console.Write("Filter by Artist (or leave blank): ");
                artist = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(artist)) artist = null;

                Console.Write("Filter by Composer (or leave blank): ");
                composer = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(composer)) composer = null;

                Console.Write("Filter by Type (or leave blank): ");
                type = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(type)) type = null;

                Console.Write("Filter by minimum Year (or leave blank): ");
                string? yearAfterInput = Console.ReadLine()?.Trim();
                yearAfter = int.TryParse(yearAfterInput, out int ya) ? ya : null;

                Console.Write("Filter by maximum Year (or leave blank): ");
                string? yearBeforeInput = Console.ReadLine()?.Trim();
                yearBefore = int.TryParse(yearBeforeInput, out int yb) ? yb : null;
            }

            // Apply filter
            List<ChordProgression> filtered = FilterProgressions(
                allProgressions, genre, period, composer, artist, type, yearAfter, yearBefore
            );

            // Print results
            if (filtered.Count == 0)
            {
                Console.WriteLine("No progressions matched the filter.");
            }
            else
            {
                ListProgressions(filtered);
            }
        }


        // Displays how often each chord transition happens as a percentage of total transitions
        static void ListChordFrequencies(List<ChordSymbol> chords, List<ChordProgression> progressions)
        {
            Dictionary<ChordPair, double> pairPercentages = GetChordPairFrequencies(chords, progressions);

            foreach (KeyValuePair<ChordPair, double> kvp in pairPercentages.OrderByDescending(kvp => kvp.Value))
            {
                Console.WriteLine($"{kvp.Key.From} -> {kvp.Key.To} : {(kvp.Value * 100):F2}%");
            }
        }

        // Displays how often each chord transtition happens as a percentage after being filtered
        static void ListFilteredChordFrequencies(List<ChordSymbol> chords, List<ChordProgression> progressions)
        {
            Dictionary<ChordPair, double> pairPercentages = GetChordPairFrequenciesWithOptionalFiltering(chords, progressions);

            foreach (KeyValuePair<ChordPair, double> kvp in pairPercentages.OrderByDescending(kvp => kvp.Value))
            {
                Console.WriteLine($"{kvp.Key.From} -> {kvp.Key.To} : {(kvp.Value * 100):F2}%");
            }
        }
    }
}
