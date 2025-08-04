using System;
using System.Collections.Generic;
using System.Linq;
using ChordProgressionGenerator.Models;
using ChordProgressionGenerator.Services;
using ChordProgressionGenerator.Utils;

// Need to add method for showing how often a particular chord goes to what other chords (print Transition Map)
    // This means splitting up the Build Progression method
// Add conversion to chord symbol
// Add Transposition capabilities
// Add modulations, which means ensuring it ends on desired chord, preferably as a cadence
// If "loop" is chosen, it needs to end on a chord that the transitionMap points back to the first chord
// Create UI

namespace ChordProgressionGenerator
{
    class Program
    {
        static void Main()
        {
            // Load chord definitions from JSON file
            ChordSymbolService chordService = new ChordSymbolService("data/chordSymbols.json");
            List<ChordSymbol> chords = chordService.LoadChords();
            ChordSymbolEditor chordEditor = new ChordSymbolEditor(chordService);

            /* List<string> addChordsTest = new List<string> { "V7/vi", "vi" };
            chordEditor.PromptToAddMissingChords(addChordsTest); */

            // Load chord progressions from JSON file
            ChordProgressionService progressionService = new ChordProgressionService("data/chordProgressions.json");
            List<ChordProgression> progressions = progressionService.LoadProgressions();

            PrintProgression(chords, progressions, chordEditor);
            //PrintTransitionMap(chords, progressions);
            //ListFilteredFirstChordFrequencies(chords, progressions);
            //ListFirstChordFrequencies(chords, progressions);
            //ListFilteredProgressions(progressions);
            //ListFilteredChordPairFrequencies(chords, progressions);
            //ListChordPairFrequences(chords, progressions);
            //ListProgressions(progressions);
            //ListChords(chords);
        }

        static Dictionary<string, string> BuildCanonicalLookup(List<ChordSymbol> chords)
        {
            Dictionary<string, string> lookup = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (ChordSymbol chord in chords)
            {
                foreach (string name in chord.AllNames())
                {
                    if (!lookup.ContainsKey(name))
                    {
                        lookup[name] = chord.RomanNumeral;
                    }
                }
            }

            return lookup;
        }

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

        public static Dictionary<ChordPair, int> CountChordPairs(
            List<ChordProgression> progressions, Dictionary<string, string> canonicalLookup)
        {
            Dictionary<ChordPair, int> pairCounts = new Dictionary<ChordPair, int>();

            foreach (ChordProgression progression in progressions)
            {
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

        // Updated to accept List<string>? for genres
        public static List<ChordProgression> FilterProgressions(
            List<ChordProgression> progressions,
            List<string>? filterGenres,
            string? period,
            string? composer,
            string? artist,
            string? type,
            int? yearAfter,
            int? yearBefore)
        {
            return progressions
                .Where(p =>
                    (filterGenres == null || (p.Genre != null && p.Genre.Any(g => filterGenres.Any(fg => fg.Equals(g, StringComparison.OrdinalIgnoreCase))))) &&
                    (string.IsNullOrEmpty(period) || p.Period?.Equals(period, StringComparison.OrdinalIgnoreCase) == true) &&
                    (string.IsNullOrEmpty(composer) || p.Composer?.Equals(composer, StringComparison.OrdinalIgnoreCase) == true) &&
                    (string.IsNullOrEmpty(artist) || p.Artist?.Equals(artist, StringComparison.OrdinalIgnoreCase) == true) &&
                    (string.IsNullOrEmpty(type) || p.Type?.Equals(type, StringComparison.OrdinalIgnoreCase) == true) &&
                    (!yearAfter.HasValue || (p.Year.HasValue && p.Year.Value >= yearAfter.Value)) &&
                    (!yearBefore.HasValue || (p.Year.HasValue && p.Year.Value <= yearBefore.Value))
                )
                .ToList();
        }

        static List<ChordProgression> GetUserFilters(List<ChordProgression> progressions)
        {
            Console.Write("Do you want to apply filters? (y/n): ");
            string? response = Console.ReadLine()?.Trim().ToLower();

            List<string>? filterGenres = null;
            string? period = null;
            string? artist = null;
            string? composer = null;
            string? type = null;
            int? yearAfter = null;
            int? yearBefore = null;

            if (response == "y")
            {
                Console.Write("Filter by Genre(s) (comma separated, or leave blank): ");
                string? genreInput = Console.ReadLine()?.Trim();
                if (!string.IsNullOrEmpty(genreInput))
                {
                    filterGenres = genreInput
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(g => g.Trim())
                        .ToList();
                }

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

            List<ChordProgression> filtered = FilterProgressions(
                progressions, filterGenres, period, composer, artist, type, yearAfter, yearBefore
            );

            return filtered;
        }

        public static Dictionary<ChordPair, double> GetFilteredChordPairFrequencies(
            List<ChordSymbol> chords,List<ChordProgression> progressions)
        {
            List<ChordProgression> filtered = GetUserFilters(progressions);
            return GetChordPairFrequencies(chords, filtered);
        }

        public static Dictionary<string, int> CountFirstChords(
            List<ChordProgression> progressions, Dictionary<string, string> canonicalLookup)
        {
            Dictionary<string, int> chordCount = new Dictionary<string, int>();

            foreach (ChordProgression progression in progressions)
            {
                List<string> flatChords = FlattenAndNormalize(progression, canonicalLookup);

                string chord = flatChords[0];

                if (chordCount.ContainsKey(chord))
                    chordCount[chord]++;
                else
                    chordCount[chord] = 1;
            }

            return chordCount;
        }

        public static Dictionary<string, double> GetFirstChordFrequencies(
            List<ChordSymbol> chords, List<ChordProgression> progressions)
        {
            Dictionary<string, string> chordCanonicalLookup = BuildCanonicalLookup(chords);
            Dictionary<string, int> rawCount = CountFirstChords(progressions, chordCanonicalLookup);

            int totalFirstChords = rawCount.Values.Sum();

            Dictionary<string, double> percentages = rawCount.ToDictionary(
                kvp => kvp.Key,
                kvp => (double)kvp.Value / totalFirstChords
            );

            return percentages;
        }

        public static Dictionary<string, double> GetFilteredFirstChordFrequencies(
            List<ChordSymbol> chords, List<ChordProgression> progressions)
        {
            List<ChordProgression> filtered = GetUserFilters(progressions);
            return GetFirstChordFrequencies(chords, filtered);
        }

        /*==========================BUILDING PROGRESSION===========================*/

        public static Dictionary<string, List<string>> BuildTransitionMap(
            List<ChordSymbol> chords,
            List<ChordProgression> progressions)
        {
            Dictionary<string, List<string>> transitionMap = new();
            Dictionary<string, string> canonicalLookup = BuildCanonicalLookup(chords);


            foreach (ChordProgression progression in progressions)
            {
                List<string> flatChords = FlattenAndNormalize(progression, canonicalLookup);

                for (int i = 0; i < flatChords.Count - 1; i++)
                {
                    string currentChord = flatChords[i];
                    string nextChord = flatChords[i + 1];

                    if (!transitionMap.ContainsKey(currentChord))
                        transitionMap[currentChord] = new List<string>();

                    transitionMap[currentChord].Add(nextChord);
                }

                if (progression.Type == "Loop" && flatChords.Count > 1)
                {
                    string lastChord = flatChords[^1];
                    string firstChord = flatChords[0];

                    if (!transitionMap.ContainsKey(lastChord))
                        transitionMap[lastChord] = new List<string>();
                    
                    transitionMap[lastChord].Add(firstChord);
                }
            }

            return transitionMap;
        }

        public static int GetDesiredLength()
        {
            Console.Write("How many chords should the progression be? (e.g., 4 or 4-6): ");
            string? input = Console.ReadLine();
            int defaultLength = 4;

            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine($"No input provided. Defaulting to {defaultLength} chords.");
                return defaultLength;
            }

            input = input.Trim();

            if (input.Contains("-"))
            {
                string[] parts = input.Split('-');

                if (parts.Length == 2 &&
                    int.TryParse(parts[0], out int min) &&
                    int.TryParse(parts[1], out int max) &&
                    min > 0 && max >= min)
                {
                    Random rng = new Random();
                    return rng.Next(min, max + 1); // inclusive upper bound
                }
                else
                {
                    Console.WriteLine($"Invalid range input. Defaulting to {defaultLength} chords.");
                    return defaultLength;
                }
            }
            else if (int.TryParse(input, out int fixedLength) && fixedLength > 0)
            {
                return fixedLength;
            }
            else
            {
                Console.WriteLine($"Invalid input. Defaulting to {defaultLength} chords.");
                return defaultLength;
            }
        }

        public static List<string> BuildProgression(List<ChordSymbol> chords, List<ChordProgression> progressions)
        {
            List<ChordProgression> filtered = GetUserFilters(progressions);
            Dictionary<string, string> chordCanonicalLookup = BuildCanonicalLookup(chords);
            Dictionary<string, List<string>> transitionMap = BuildTransitionMap(chords, filtered);
            int length = GetDesiredLength();
            List<string> firstChords = new List<string>();

            foreach (ChordProgression progression in filtered)
            {
                List<string> flatChords = FlattenAndNormalize(progression, chordCanonicalLookup);

                if (flatChords.Count == 0) continue;
                
                firstChords.Add(flatChords[0]);
            }

            Random rng = new Random();

            string current = firstChords[rng.Next(firstChords.Count)];
            List<string> newProgression = new List<string> { current };

            for (int i = 1; i < length; i++)
            {
                if (!transitionMap.ContainsKey(current) || transitionMap[current].Count == 0)
                {
                    current = firstChords[rng.Next(firstChords.Count)];
                    newProgression.Add(current);
                    continue;
                }
                
                List<string> nextChord = transitionMap[current];
                current = nextChord[rng.Next(nextChord.Count)];
                newProgression.Add(current);
            }
            return newProgression;
        }

        static void PrintProgression(List<ChordSymbol> chords, List<ChordProgression> progressions, ChordSymbolEditor chordEditor)
        {
            List<string> newProgression = BuildProgression(chords, progressions);
            Console.WriteLine(string.Join(", ", newProgression));
            chordEditor.PromptToAddMissingChords(newProgression);
        }

        /*============================TESTING MODULES===============================*/

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

                if (prog.Genre != null && prog.Genre.Count > 0)
                    Console.WriteLine($"Genre: {string.Join(", ", prog.Genre)}");

                if (!string.IsNullOrWhiteSpace(prog.Type))
                    Console.WriteLine($"Type: {prog.Type ?? "Unknown"}");

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
            List<ChordProgression> filtered = GetUserFilters(allProgressions);

            if (filtered.Count == 0)
            {
                Console.WriteLine("No progressions matched the filter.");
            }
            else
            {
                ListProgressions(filtered);
            }
        }

        static void ListChordPairFrequences(List<ChordSymbol> chords, List<ChordProgression> progressions)
        {
            Dictionary<ChordPair, double> pairPercentages = GetChordPairFrequencies(chords, progressions);

            foreach (KeyValuePair<ChordPair, double> kvp in pairPercentages.OrderByDescending(kvp => kvp.Value))
            {
                Console.WriteLine($"{kvp.Key.From} -> {kvp.Key.To} : {(kvp.Value * 100):F2}%");
            }
        }

        static void ListFilteredChordPairFrequencies(List<ChordSymbol> chords, List<ChordProgression> progressions)
        {
            Dictionary<ChordPair, double> pairPercentages = GetFilteredChordPairFrequencies(chords, progressions);

            foreach (KeyValuePair<ChordPair, double> kvp in pairPercentages.OrderByDescending(kvp => kvp.Value))
            {
                Console.WriteLine($"{kvp.Key.From} -> {kvp.Key.To} : {(kvp.Value * 100):F2}%");
            }
        }

        static void ListFirstChordFrequencies(List<ChordSymbol> chords, List<ChordProgression> progressions)
        {
            Dictionary<string, double> chordPercentages = GetFirstChordFrequencies(chords, progressions);

            foreach (KeyValuePair<string, double> kvp in chordPercentages.OrderByDescending(kvp => kvp.Value))
            {
                Console.WriteLine($"{kvp.Key} : {(kvp.Value * 100):F2}%");
            }
        }

        static void ListFilteredFirstChordFrequencies(List<ChordSymbol> chords, List<ChordProgression> progressions)
        {
            Dictionary<string, double> chordPercentages = GetFilteredFirstChordFrequencies(chords, progressions);

            foreach (KeyValuePair<string, double> kvp in chordPercentages.OrderByDescending(kvp => kvp.Value))
            {
                Console.WriteLine($"{kvp.Key} : {(kvp.Value * 100):F2}%");
            }
        }

        public static void PrintTransitionMap(List<ChordSymbol> chords, List<ChordProgression> progressions)
        {
            List<ChordProgression> filtered = GetUserFilters(progressions);
            Dictionary<string, List<string>> transitionMap = BuildTransitionMap(chords, filtered);

            Console.WriteLine("=== TRANSITION MAP ===\n");

            foreach (var entry in transitionMap.OrderBy(kvp => kvp.Key))
            {
                var groupedTargets = entry.Value.GroupBy(x => x)
                                                .Select(g => $"{g.Key} ({g.Count()})")
                                                .OrderBy(s => s)
                                                .ToList();

                int totalTransitions = entry.Value.Count;

                Console.WriteLine($"{entry.Key} -> {string.Join(", ", groupedTargets)}, Total: {totalTransitions}");
            }
        }


    }
}
