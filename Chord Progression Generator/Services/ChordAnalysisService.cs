using System;
using System.Collections.Generic;
using System.Linq;
using ChordProgressionGenerator.Models;

namespace ChordProgressionGenerator.Services
{
    public class ChordAnalysisService
    {
        // Count how many times each chord pair occurs in given progressions
        public Dictionary<ChordPair, int> CountChordPairs(List<ChordProgression> progressions, Dictionary<string, string> canonicalLookup)
        {
            Dictionary<ChordPair, int> pairCounts = new();

            foreach (ChordProgression progression in progressions)
            {
                List<string> flatChords = FlattenAndNormalize(progression, canonicalLookup);

                for (int i = 0; i < flatChords.Count - 1; i++)
                {
                    var pair = new ChordPair(flatChords[i], flatChords[i + 1]);
                    pairCounts.TryGetValue(pair, out int count);
                    pairCounts[pair] = count + 1;
                }

                if (progression.Type == "Loop" && flatChords.Count > 1)
                {
                    var loopPair = new ChordPair(flatChords[^1], flatChords[0]);
                    pairCounts.TryGetValue(loopPair, out int count);
                    pairCounts[loopPair] = count + 1;
                }
            }

            return pairCounts;
        }

        // Calculate frequency (%) for chord pairs
        public Dictionary<ChordPair, double> GetChordPairFrequencies(List<ChordSymbol> chords, List<ChordProgression> progressions)
        {
            var canonicalLookup = BuildCanonicalLookup(chords);
            var rawCounts = CountChordPairs(progressions, canonicalLookup);

            int totalPairs = rawCounts.Values.Sum();
            if (totalPairs == 0) return new Dictionary<ChordPair, double>();

            return rawCounts.ToDictionary(kvp => kvp.Key, kvp => (double)kvp.Value / totalPairs);
        }

        // Build a transition map: chord -> list of possible next chords
        public Dictionary<string, List<string>> BuildTransitionMap(List<ChordSymbol> chords, List<ChordProgression> progressions)
        {
            Dictionary<string, List<string>> transitionMap = new();
            Dictionary<string, string> canonicalLookup = BuildCanonicalLookup(chords);

            foreach (var progression in progressions)
            {
                var flatChords = FlattenAndNormalize(progression, canonicalLookup);

                for (int i = 0; i < flatChords.Count - 1; i++)
                {
                    string current = flatChords[i];
                    string next = flatChords[i + 1];

                    if (!transitionMap.ContainsKey(current))
                        transitionMap[current] = new List<string>();

                    transitionMap[current].Add(next);
                }

                if (progression.Type == "Loop" && flatChords.Count > 1)
                {
                    string last = flatChords[^1];
                    string first = flatChords[0];
                    if (!transitionMap.ContainsKey(last))
                        transitionMap[last] = new List<string>();
                    transitionMap[last].Add(first);
                }
            }

            return transitionMap;
        }

        // Count how many times each chord appears first in progressions
        public Dictionary<string, int> CountFirstChords(List<ChordProgression> progressions, Dictionary<string, string> canonicalLookup)
        {
            Dictionary<string, int> chordCount = new();

            foreach (var prog in progressions)
            {
                var flatChords = FlattenAndNormalize(prog, canonicalLookup);
                if (flatChords.Count == 0) continue;

                string firstChord = flatChords[0];
                chordCount.TryGetValue(firstChord, out int count);
                chordCount[firstChord] = count + 1;
            }

            return chordCount;
        }

        // Calculate frequency (%) of first chords
        public Dictionary<string, double> GetFirstChordFrequencies(List<ChordSymbol> chords, List<ChordProgression> progressions)
        {
            var canonicalLookup = BuildCanonicalLookup(chords);
            var rawCount = CountFirstChords(progressions, canonicalLookup);

            int total = rawCount.Values.Sum();
            if (total == 0) return new Dictionary<string, double>();

            return rawCount.ToDictionary(kvp => kvp.Key, kvp => (double)kvp.Value / total);
        }

        // Helper: flatten progression bars and normalize chord names by canonical lookup
        public List<string> FlattenAndNormalize(ChordProgression progression, Dictionary<string, string> canonicalLookup)
        {
            List<string> flat = new();

            foreach (var bar in progression.Bars)
            {
                foreach (var beat in bar)
                {
                    foreach (var chord in beat)
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

        // Helper: build canonical lookup from chord names to their Roman numeral
        public Dictionary<string, string> BuildCanonicalLookup(List<ChordSymbol> chords)
        {
            var lookup = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var chord in chords)
            {
                foreach (var name in chord.AllNames())
                {
                    if (!lookup.ContainsKey(name))
                        lookup[name] = chord.RomanNumeral;
                }
            }

            return lookup;
        }
    }
}
