using System;
using System.Collections.Generic;
using System.Linq;
using ChordProgressionGenerator.Models;
using ChordProgressionGenerator.Utils;

namespace ChordProgressionGenerator.Services
{
    public class ProgressionBuilderService
    {

        private readonly ProgressionFilterService _filterService;
        private readonly ChordSymbolService _chordSymbolService;
        private readonly ChordAnalysisService _analysisService;
        //private readonly ChordProgressionService _progressionService;
        private Dictionary<ChordPair, double> transitionFrequencies = new();
        private List<ChordSymbol> _chords = new();
        private List<ChordProgression> _progressions = new();
        private readonly ChordSymbolEditor _chordSymbolEditor;

        public ProgressionBuilderService(
            ChordAnalysisService analysisService, 
            ProgressionFilterService filterService,
            ChordSymbolService chordSymbolService,
            ChordSymbolEditor chordSymbolEditor,
            ChordProgressionService _progressionService)
        {
            _analysisService = new ChordAnalysisService(chordSymbolService);
            _filterService = filterService;
            _chordSymbolService = chordSymbolService;
            _chords = _chordSymbolService.LoadChords();
            _chordSymbolEditor = chordSymbolEditor;
            _progressionService = new ChordProgressionService("data/chordProgressions.json");
            _progressions = _progressionService.LoadProgressions();
        }
        // Prompts user for progression length, supports fixed or range
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

        // Build a random progression using filtered progressions and transitions
        public List<string> BuildProgression(List<ChordSymbol> chords, List<ChordProgression> progressions)
        {
            List<ChordProgression> filtered = _filterService.GetUserFilters(progressions);
            Dictionary<string, string> canonicalLookup = _analysisService.BuildCanonicalLookup(chords);
            Dictionary<string, List<string>> transitionMap = _analysisService.GetForwardTransitions(chords, filtered);
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

        //Temporarily returning all possible modulations
        public List<List<string>> BuildModulationV2(string start, string target, List<ChordSymbol> chords, List<ChordProgression> progressions)
        {
            int length = GetDesiredLength();
            List<List<string>> failedPaths = new();
            List<List<string>> successfulPaths = new();

            int numOfDirectAttempts = 0;
            //Filters for progressions that start with correct chord
            do
            {
                List<List<string>> modulationSearch = DirectModulationPaths(target, length, chords, progressions);
                foreach (List<string> search in modulationSearch)
                {
                    if (search[0] == start){
                        successfulPaths.Add(search);
                    }
                    else {
                        failedPaths.Add(search);
                    }
                }
                numOfDirectAttempts++;
            } while (successfulPaths.Count == 0 && numOfDirectAttempts < 10);
            Console.WriteLine($"Total Direct Attempts: {numOfDirectAttempts * 10}");

            int numOfCommonToneAttempts = 0;
            if (successfulPaths.Count == 0)
            {
                do
                {
                    List<List<string>> modulationSearch = CommonToneModulationPaths(target, length, chords, progressions);
                    foreach (List<string> search in modulationSearch)
                    {
                        if (search[0] == start && search.Count == length){
                            successfulPaths.Add(search);
                        } else {
                            failedPaths.Add(search);
                        }
                    }
                    numOfCommonToneAttempts++;
                } while (successfulPaths.Count == 0 && numOfCommonToneAttempts < 10);
                Console.WriteLine($"Total Common Tone Attempts: {numOfCommonToneAttempts * 10}");
            }


            return successfulPaths;
        }



        // Just produces random progressions of the correct length that end on target chord
        public List<List<string>> DirectModulationPaths(string target, int length, List<ChordSymbol> chords, List<ChordProgression> progressions)
        {
            List<List<string>> pathsToTarget = new();

            int numOfAttempts = 10;
            for (int i = 0; i < numOfAttempts; i++)
            {
                pathsToTarget.Add(BuildBackwardPath(target, length, chords, progressions));
            }

            return pathsToTarget;
        }

        public List<List<string>> CommonToneModulationPaths(string target, int length, List<ChordSymbol> chords, List<ChordProgression> progressions)
        {
            int totalLength = length;
            int divergePathPoint = totalLength / 2;
            List<List<string>> pathsToTarget = new();

            int numOfAttempts = 10;
            for (int i = 0; i < numOfAttempts; i++)
            {
                List<string> endOfProgression = BuildBackwardPath(target, divergePathPoint, chords, progressions);
                List<string> transitionCandidates = FindSimilarChords(endOfProgression[0], chords);
                int _randomChordToUse = _random.Next(0, transitionCandidates.Count);
                List<string> beginningOfProgression = BuildBackwardPath(transitionCandidates[_randomChordToUse], totalLength - divergePathPoint, chords, progressions);

                //List<string> fullPath = beginningOfProgression.AddRange(endOfProgression);
                pathsToTarget.Add(beginningOfProgression.Concat(endOfProgression).ToList());
            }

            return pathsToTarget;
        }

        public List<string> FindSimilarChords(string transitionChord, List<ChordSymbol> chords)
        {
            int _randomChordToUse = _random.Next(0, chords.Count);
            ChordSymbol chordToUse = chords[_randomChordToUse];
            List<string> similarChords = new();
            if (_chordSymbolService.FindByName(transitionChord) == null)
            {
                similarChords.Add(chordToUse.RomanNumeral);
                return similarChords;
            }

            chordToUse = _chordSymbolService.FindByName(transitionChord)!;

            foreach (var chord in chords)
            {
                double proximityScore = _analysisService.GetProximityScore(chordToUse, chord);
                if (proximityScore >= 0.8)
                {
                    similarChords.Add(chord.RomanNumeral);
                }
            }

            return similarChords;
        }



        public void InitializeTransitionFrequencies(List<ChordSymbol> chords, List<ChordProgression> progressions)
        {
            transitionFrequencies = _analysisService.BuildTransitionFrequencies(chords, progressions);
        }

        /* public List<string> BuildModulationProgression(string start, string end, List<ChordSymbol> chords, List<ChordProgression> progressions)
        {
            _chords = chords;
            _progressions = progressions;
            int totalLength = GetDesiredLength();

            if (totalLength < 4)
                throw new ArgumentException("Modulation progression must be at least 4 chords long.");

            int bridgeLength = 1;
            int forwardLength = (totalLength - bridgeLength) / 2;
            int backwardLength = totalLength - bridgeLength - forwardLength;

            List<string> forward = BuildForwardPath(start, forwardLength);
            List<string> backward = BuildBackwardPath(end, backwardLength);

            string forwardEnd = forward.LastOrDefault() ?? start;
            string backwardStart = backward.FirstOrDefault() ?? end; */

            // Collect unresolved chords
            /* List<string> unresolved = new();

            if (_chordSymbolService.FindByName(forwardEnd) == null)
                unresolved.Add(forwardEnd);

            if (_chordSymbolService.FindByName(backwardStart) == null)
                unresolved.Add(backwardStart);

            if (unresolved.Count > 0)
            {

                Console.WriteLine("Unrecognized chords, do you want to edit? (y/n)");
                string? userWantsToEdit = Console.ReadLine()?.Trim().ToLower();

                if (userWantsToEdit == "y")
                {
                    _chordSymbolEditor.PromptToAddMissingChords(unresolved);
                    // User added or edited chords, retry build after refresh
                    // return BuildModulationProgression(start, end, chords, progressions);
                }
                else
                {
                    Console.WriteLine("Failed to build modulation due to unresolved chords.");
                    while (true)
                    {
                        Console.Write("Retry? (y/n): ");
                        string? input = Console.ReadLine()?.Trim().ToLower();

                        if (input == "y")
                        {
                            // Retry the build
                            return BuildModulationProgression(start, end, chords, progressions);
                        }
                        else if (input == "n")
                        {
                            throw new InvalidOperationException("Modulation build aborted by user due to unresolved chords.");
                        }
                        else
                        {
                            Console.WriteLine("Please enter 'y' or 'n'.");
                        }
                    }
                }
            } */

            /* // Build the bridge chords
            List<string> bridge = BuildBridgeChords(forwardEnd, backwardStart, bridgeLength);

            // Pad the bridge if it returned fewer chords than bridgeLength
            while (bridge.Count < bridgeLength)
            {
                bridge.Add(backwardStart); // Could be another fallback chord or proximity filler
            }

            List<string> progression = new();
            progression.AddRange(forward);
            progression.AddRange(bridge);
            progression.AddRange(backward);

            if (progression.Count != totalLength)
                throw new InvalidOperationException($"Progression length mismatch. Got {progression.Count}, expected {totalLength}.");

            return progression;
        }*/

        private readonly Random _random = new Random();

        private T PickWeightedRandom<T>(Dictionary<T, double> weightedOptions) where T : notnull
        {
            double totalWeight = weightedOptions.Values.Sum();
            double roll = _random.NextDouble() * totalWeight;
            double cumulative = 0.0;

            foreach (var kvp in weightedOptions)
            {
                cumulative += kvp.Value;
                if (roll <= cumulative)
                    return kvp.Key;
            }

            return weightedOptions.Keys.Last(); // fallback
        } 



        private List<string> BuildForwardPath(string start, int length)
        {
            List<string> path = new() { start };

            for (int i = 0; i < length - 1; i++)
            {
                List<ChordSymbol> pathSoFar = path
                    .Select(name => _chordSymbolService.FindByName(name))
                    .Where(cs => cs != null)
                    .Cast<ChordSymbol>()
                    .ToList();

                Dictionary<string, List<string>> transitions = _analysisService.GetForwardTransitions(pathSoFar, _progressions);

                string currentChord = path.Last();

                if (!transitions.ContainsKey(currentChord) || transitions[currentChord].Count == 0)
                    break;

                // Count frequencies of each possible next chord
                Dictionary<string, double> weightedChoices = transitions[currentChord]
                    .GroupBy(chord => chord)
                    .ToDictionary(g => g.Key, g => (double)g.Count());

                // Optional: avoid immediate repetition
                weightedChoices.Remove(currentChord);

                if (weightedChoices.Count == 0)
                    break;

                string next = PickWeightedRandom(weightedChoices);
                path.Add(next);
            }

            return path;
        }



        public List<string> BuildBackwardPath(string target, int length, List<ChordSymbol> chords, List<ChordProgression> progressions)
        {
            Stack<string> path = new();
            path.Push(target);

            for (int i = 0; i < length - 1; i++)
            {
                List<ChordSymbol> pathSoFar = path.Reverse()
                    .Select(name => _chordSymbolService.FindByName(name))
                    .Where(cs => cs != null)
                    .Cast<ChordSymbol>()
                    .ToList();

                Dictionary<string, List<string>> transitions = _analysisService.GetBackwardTransitions(chords, progressions);

                string currentChord = path.Peek();

                if (!transitions.ContainsKey(currentChord) || transitions[currentChord].Count == 0)
                    break;

                Dictionary<string, int> weightedChoices = transitions[currentChord]
                    .GroupBy(chord => chord)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Optional: avoid immediate repetition
                weightedChoices.Remove(currentChord);

                if (weightedChoices.Count == 0)
                    break;

                string prev = PickWeightedRandom(weightedChoices.ToDictionary(kvp => kvp.Key, kvp => (double)kvp.Value));
                path.Push(prev);
            }

            return path.ToList();
        }


        private List<string> BuildBridgeChords(string from, string to, int bridgeLength)
        {
            if (bridgeLength == 0) return new();

            ChordSymbol? fromSymbol = _chordSymbolService.FindByName(from);
            ChordSymbol? toSymbol = _chordSymbolService.FindByName(to);

            if (fromSymbol == null || toSymbol == null)
                return new(); // fallback if chords are unknown

            // Get all unique chords and exclude the target chord `to`
            List<string> candidates = _analysisService.GetAllUniqueChords(_progressions)
                .Where(chord =>
                {
                    if (chord == to) return false;

                    ChordSymbol? candidateSymbol = _chordSymbolService.FindByName(chord);
                    if (candidateSymbol == null) return false;

                    return ChordUtils.AreChordsEnharmonicallyClose(chord, from) ||
                        _analysisService.GetProximityScore(candidateSymbol, fromSymbol) > 0.3;
                })
                .ToList();

            // Sort candidates by proximity to the target chord `to`
            List<string> sorted = candidates
                .OrderByDescending(chord =>
                {
                    ChordSymbol? symbol = _chordSymbolService.FindByName(chord);
                    return symbol != null ? _analysisService.GetProximityScore(symbol, toSymbol) : 0;
                })
                .ToList();

            if (bridgeLength == 1)
            {
                return new() { sorted.FirstOrDefault() ?? to };
            }
            else
            {
                return sorted.Take(bridgeLength).ToList();
            }
        }

    }
}
