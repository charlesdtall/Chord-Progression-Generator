using System;
using System.Collections.Generic;
using System.Linq;
using ChordProgressionGenerator.Models;
using ChordProgressionGenerator.Utils;

namespace ChordProgressionGenerator.Services
{
    public class ChordAnalysisService
    {

        private readonly ChordSymbolService _chordSymbolService;
        public ChordAnalysisService(ChordSymbolService chordSymbolService)
        {
            _chordSymbolService = chordSymbolService;
        }


        /// <summary>
        /// Builds a dictionary mapping each chord symbol to a list of possible next chords (forward transitions),
        /// derived from the supplied progressions.
        /// </summary>
        public Dictionary<string, List<string>> GetForwardTransitions(List<ChordSymbol> chords, List<ChordProgression> progressions)
        {
            var forwardMap = new Dictionary<string, List<string>>();
            Dictionary<string, string> canonicalLookup = BuildCanonicalLookup(chords);

            foreach (var progression in progressions)
            {
                List<string> chordSequence = FlattenAndNormalize(progression, canonicalLookup);

                for (int i = 0; i < chordSequence.Count - 1; i++)
                {
                    string from = chordSequence[i];
                    string to = chordSequence[i + 1];

                    if (!forwardMap.ContainsKey(from))
                        forwardMap[from] = new List<string>();

                    forwardMap[from].Add(to);
                }

                if (progression.Type == "Loop" && chordSequence.Count > 1)
                {
                    string last = chordSequence[^1]; // last chord
                    string first = chordSequence[0]; // first chord

                    if (!forwardMap.ContainsKey(last))
                        forwardMap[last] = new List<string>();

                    forwardMap[last].Add(first);
                }
                
            }

            return forwardMap;
        }

        /// <summary>
        /// Builds a dictionary mapping each chord symbol to a list of possible previous chords (backward transitions),
        /// derived from the supplied progressions.
        /// </summary>
        public Dictionary<string, List<string>> GetBackwardTransitions(List<ChordSymbol> chords, List<ChordProgression> progressions)
        {
            var backwardMap = new Dictionary<string, List<string>>();
            Dictionary<string, string> canonicalLookup = BuildCanonicalLookup(chords);

            foreach (var progression in progressions)
            {
                List<string> chordSequence = FlattenAndNormalize(progression, canonicalLookup);

                for (int i = 1; i < chordSequence.Count; i++)
                {
                    string to = chordSequence[i];
                    string from = chordSequence[i - 1];

                    if (!backwardMap.ContainsKey(to))
                        backwardMap[to] = new List<string>();

                    backwardMap[to].Add(from);
                }

                if (progression.Type == "Loop" && chordSequence.Count > 1)
                {
                    string last = chordSequence[^1];
                    string first = chordSequence[0];


                    if (!backwardMap.ContainsKey(first))
                        backwardMap[first] = new List<string>();

                    backwardMap[first].Add(last);
                }
            }

            return backwardMap;
        }

        /// <summary>
        /// Returns frequencies of first chords in the supplied progressions as a dictionary of chord -> frequency (0 to 1).
        /// </summary>
        public Dictionary<string, double> GetFirstChordFrequencies(List<ChordSymbol> chords, List<ChordProgression> progressions)
        {
            var counts = new Dictionary<string, int>();
            int total = 0;
            Dictionary<string, string> canonicalLookup = BuildCanonicalLookup(chords);

            foreach (var progression in progressions)
            {
                List<string> chordSequence = FlattenAndNormalize(progression, canonicalLookup);

                if (chordSequence.Count > 0)
                {
                    string firstChord = chordSequence[0];
                    if (!counts.ContainsKey(firstChord))
                        counts[firstChord] = 0;

                    counts[firstChord]++;
                    total++;
                }
            }

            return counts.ToDictionary(kv => kv.Key, kv => (double)kv.Value / total);
        }

        /// <summary>
        /// Returns a list of all unique chord symbols found in the provided progressions.
        /// </summary>
        public List<string> GetAllUniqueChords(List<ChordProgression> progressions)
        {
            var uniqueChords = new HashSet<string>();

            foreach (var progression in progressions)
            {
                List<string> chordSequence = ExtractChordSequence(progression);
                foreach (var chord in chordSequence)
                {
                    uniqueChords.Add(chord);
                }
            }

            return uniqueChords.ToList();
        }

        /// <summary>
        /// Builds a canonical lookup dictionary from chord symbol to Roman numeral (or symbol fallback).
        /// </summary>
        public Dictionary<string, string> BuildCanonicalLookup(List<ChordSymbol> chords)
        {
            var lookup = new Dictionary<string, string>();

            foreach (var chord in chords)
            {
                if (!string.IsNullOrWhiteSpace(chord.Symbol))
                {
                    lookup[chord.Symbol] = string.IsNullOrWhiteSpace(chord.RomanNumeral) ? chord.Symbol : chord.RomanNumeral;
                }
            }

            return lookup;
        }

        /* /// <summary>
        /// Builds a transition map of chord symbol to list of possible next chord symbols based on filtered progressions.
        /// </summary>
        public Dictionary<string, List<string>> BuildTransitionMap(List<ChordSymbol> chords, List<ChordProgression> filteredProgressions)
        {
            var transitionMap = new Dictionary<string, List<string>>();

            foreach (var progression in filteredProgressions)
            {
                List<string> chordSequence = ExtractChordSequence(progression);

                for (int i = 0; i < chordSequence.Count - 1; i++)
                {
                    string from = chordSequence[i];
                    string to = chordSequence[i + 1];

                    if (!transitionMap.ContainsKey(from))
                        transitionMap[from] = new List<string>();

                    transitionMap[from].Add(to);
                }
            }

            return transitionMap;
        } */

        /// <summary>
        /// Flattens a progression into a normalized list of chords using a canonical lookup dictionary.
        /// </summary>
        public List<string> FlattenAndNormalize(ChordProgression progression, Dictionary<string, string> canonicalLookup)
        {
            var flattened = new List<string>();

            foreach (var bar in progression.Bars)
            {
                foreach (var beatGroup in bar)
                {
                    foreach (var chord in beatGroup)
                    {
                        if (canonicalLookup.TryGetValue(chord, out string? normalized))
                            flattened.Add(normalized);
                        else
                            flattened.Add(chord);
                    }
                }
            }

            return flattened;
        }

        /// <summary>
        /// Builds a dictionary of chord pair frequencies (probabilities) from the given chords and progressions.
        /// </summary>
        public Dictionary<ChordPair, double> BuildTransitionFrequencies(List<ChordSymbol> chords, List<ChordProgression> progressions)
        {
            var pairCounts = new Dictionary<ChordPair, int>();
            var fromCounts = new Dictionary<string, int>();
            Dictionary<string, string> canonicalLookup = BuildCanonicalLookup(chords);

            foreach (var progression in progressions)
            {
                List<string> chordSequence = FlattenAndNormalize(progression, canonicalLookup);

                for (int i = 0; i < chordSequence.Count - 1; i++)
                {
                    var from = chordSequence[i];
                    var to = chordSequence[i + 1];
                    var pair = new ChordPair(from, to);

                    if (!pairCounts.ContainsKey(pair))
                        pairCounts[pair] = 0;
                    pairCounts[pair]++;

                    if (!fromCounts.ContainsKey(from))
                        fromCounts[from] = 0;
                    fromCounts[from]++;
                }
            }

            var frequencies = new Dictionary<ChordPair, double>();

            foreach (var kvp in pairCounts)
            {
                double frequency = fromCounts.TryGetValue(kvp.Key.From, out int count) && count > 0
                    ? (double)kvp.Value / count
                    : 0.0;

                frequencies[kvp.Key] = frequency;
            }

            return frequencies;
        }

        /// <summary>
        /// Calculates a proximity score between two chords based on note overlap.
        /// This is a simple heuristic: the more shared notes, the closer the chords.
        /// </summary>
        public double GetProximityScore(ChordSymbol chord1, ChordSymbol chord2)
        {
            if (chord1 == null || chord2 == null) return 0.0;

            // Step 1: Normalize and resolve roots
            string root1 = GetResolvedRootNote(chord1.Symbol);
            string root2 = GetResolvedRootNote(chord2.Symbol);

            int root1Pitch = NoteHelper.NoteToInt(root1);
            int root2Pitch = NoteHelper.NoteToInt(root2);

            int rootDistance = Math.Min(
                (root1Pitch - root2Pitch + 12) % 12,
                (root2Pitch - root1Pitch + 12) % 12); // Always shortest distance

            double rootCloseness = 1.0 - (rootDistance / 6.0); // Normalize to [0.0, 1.0]

            // Step 2: Shared note overlap (as before)
            var notes1 = new HashSet<string>(chord1.Notes.Select(n => NormalizeNoteName(n)));
            var notes2 = new HashSet<string>(chord2.Notes.Select(n => NormalizeNoteName(n)));

            int sharedNotesCount = notes1.Intersect(notes2).Count();
            int totalNotes = notes1.Union(notes2).Count();

            double noteOverlap = (totalNotes > 0) ? (double)sharedNotesCount / totalNotes : 0.0;

            // Step 3: Chord quality similarity (major/minor/diminished/etc)
            double qualityScore = GetChordQualityScore(chord1.Symbol, chord2.Symbol); // Between 0.0 and 1.0

            // Step 4: Combine scores (tweak weights as needed)
            double score = (0.2 * rootCloseness) + (0.7 * noteOverlap) + (0.1 * qualityScore);
            return score;
        }

        private string GetResolvedRootNote(string chordSymbol)
        {
            if (chordSymbol.Contains("/"))
                return ChordUtils.GetSecondaryChordRoot(chordSymbol); // e.g., V/V → D

            return ChordUtils.RomanToNote(chordSymbol); // Plain root of chord like "C#m7"
        }

        private double GetChordQualityScore(string symbol1, string symbol2)
        {
            string quality1 = ExtractChordQuality(symbol1);
            string quality2 = ExtractChordQuality(symbol2);

            return (quality1 == quality2) ? 1.0 : 0.0;
        }

        private string ExtractChordQuality(string symbol)
        {
            // Very naive for now — customize as needed
            if (symbol.Contains("o")) return "dim";
            if (symbol.Contains("m")) return "min";
            return "maj";
        }




        /// <summary>
        /// Normalizes a note name for enharmonic equivalence.
        /// For example, converts "Db" to "C#", etc.
        /// This helps treat enharmonic equivalents as equal.
        /// </summary>
        public string NormalizeNoteName(string note)
        {
            // Simple dictionary for common enharmonic equivalents.
            // Extend as needed.
            var enharmonicMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Db", "C#" },
                { "Eb", "D#" },
                { "Fb", "E" },
                { "Gb", "F#" },
                { "Ab", "G#" },
                { "Bb", "A#" },
                { "Cb", "B" },
                { "E#", "F" },
                { "B#", "C" }
            };

            if (enharmonicMap.TryGetValue(note, out string? normalized))
                return normalized;

            return note;
        }

        /// <summary>
        /// Helper method to extract a flat list of chord symbols from a ChordProgression's Bars.
        /// </summary>
        private List<string> ExtractChordSequence(ChordProgression progression)
        {
            var chordList = new List<string>();

            foreach (var bar in progression.Bars)
            {
                foreach (var beatGroup in bar)
                {
                    chordList.AddRange(beatGroup);
                }
            }

            return chordList;
        }
    }
}
