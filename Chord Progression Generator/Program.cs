using System;
using System.Collections.Generic;
using System.Linq;
using ChordProgressionGenerator.Models;
using ChordProgressionGenerator.Services;
using ChordProgressionGenerator.Utils;

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

        static Dictionary<string, string> BuildCanonicalLookup(List<ChordSymbol> chords)
        {
            Dictionary<string, string> lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

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

        // Updated to pass List<string>? for genres
        public static Dictionary<ChordPair, double> GetFilteredChordPairFrequencies(
            List<ChordSymbol> chords,
            List<ChordProgression> allProgressions,
            List<string>? filterGenres,
            string? period,
            string? artist,
            string? composer,
            string? type,
            int? yearAfter,
            int? yearBefore)
        {
            List<ChordProgression> filtered = FilterProgressions(allProgressions, filterGenres, period, composer, artist, type, yearAfter, yearBefore);
            return GetChordPairFrequencies(chords, filtered);
        }

        // Updated user prompt to read list of genres and pass List<string> to filtering
        public static Dictionary<ChordPair, double> GetChordPairFrequenciesWithOptionalFiltering(
            List<ChordSymbol> chords, List<ChordProgression> progressions)
        {
            Console.Write("Do you want to apply filters? (y/n): ");
            string? response = Console.ReadLine()?.Trim().ToLower();

            if (response != "y")
            {
                return GetChordPairFrequencies(chords, progressions);
            }

            Console.Write("Filter by Genre(s) (comma separated, or leave blank): ");
            string? genreInput = Console.ReadLine()?.Trim();

            List<string>? filterGenres = null;
            if (!string.IsNullOrEmpty(genreInput))
            {
                filterGenres = genreInput
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(g => g.Trim())
                    .ToList();
            }

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

            return GetFilteredChordPairFrequencies(chords, progressions, filterGenres, period, artist, composer, type, yearAfter, yearBefore);
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
                allProgressions, filterGenres, period, composer, artist, type, yearAfter, yearBefore
            );

            if (filtered.Count == 0)
            {
                Console.WriteLine("No progressions matched the filter.");
            }
            else
            {
                ListProgressions(filtered);
            }
        }

        static void ListChordFrequencies(List<ChordSymbol> chords, List<ChordProgression> progressions)
        {
            Dictionary<ChordPair, double> pairPercentages = GetChordPairFrequencies(chords, progressions);

            foreach (KeyValuePair<ChordPair, double> kvp in pairPercentages.OrderByDescending(kvp => kvp.Value))
            {
                Console.WriteLine($"{kvp.Key.From} -> {kvp.Key.To} : {(kvp.Value * 100):F2}%");
            }
        }

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
