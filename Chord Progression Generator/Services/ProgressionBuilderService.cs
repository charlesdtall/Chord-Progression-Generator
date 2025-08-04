using System;
using System.Collections.Generic;
using ChordProgressionGenerator.Models;
using ChordProgressionGenerator.Services;

namespace ChordProgressionGenerator.Services
{
    public class ProgressionBuilderService
    {
        private readonly ChordAnalysisService _analysisService;

        public ProgressionBuilderService(ChordAnalysisService analysisService)
        {
            _analysisService = analysisService;
        }

        // Prompt user for progression length, supporting fixed or range (e.g., "4" or "3-6")
        public int GetDesiredLength()
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
                    Random rng = new();
                    return rng.Next(min, max + 1);
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

        // Build a chord progression randomly using the transition map and filtered progressions
        public List<string> BuildProgression(List<ChordSymbol> chords, List<ChordProgression> progressions)
        {
            List<ChordProgression> filtered = Program.GetUserFilters(progressions);
            Dictionary<string, string> canonicalLookup = _analysisService.BuildCanonicalLookup(chords);
            Dictionary<string, List<string>> transitionMap = _analysisService.BuildTransitionMap(chords, filtered);
            int length = GetDesiredLength();

            List<string> firstChords = new();

            foreach (var prog in filtered)
            {
                List<string> flatChords = _analysisService.FlattenAndNormalize(prog, canonicalLookup);
                if (flatChords.Count > 0)
                    firstChords.Add(flatChords[0]);
            }

            if (firstChords.Count == 0)
            {
                Console.WriteLine("No suitable progressions found to build from.");
                return new List<string>();
            }

            Random rng = new();
            string current = firstChords[rng.Next(firstChords.Count)];
            List<string> newProgression = new() { current };

            for (int i = 1; i < length; i++)
            {
                if (!transitionMap.ContainsKey(current) || transitionMap[current].Count == 0)
                {
                    current = firstChords[rng.Next(firstChords.Count)];
                    newProgression.Add(current);
                    continue;
                }

                List<string> nextChords = transitionMap[current];
                current = nextChords[rng.Next(nextChords.Count)];
                newProgression.Add(current);
            }

            return newProgression;
        }
    }
}
