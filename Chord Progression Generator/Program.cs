using System;
using System.Collections.Generic;
using System.Linq;
using ChordProgressionGenerator.Models;
using ChordProgressionGenerator.Services;
using ChordProgressionGenerator.Utils;


//NEED TO MAKE SURE MODULATION CAN TAKE IN SYMBOLS TOO
//ADD MAX REGER MODULATIONS JUST HAVE SOMETHIN' IN THERE FOR EVERYTHING
//ADD FILTER ABILITY TO MODULATIONS
//ADD CLI FOR USE ENTERING CHORDS FOR MODULATION
//ADD LOOP BUILDING (BASICALLY I-I MODULATION FILTERED BY LOOP TYPE???)
//ADDING SEQUENCES IS SLIGHTLY LONGER TERM, REQUIRES TRANSPOSITION LOGIC I HAVEN'T BEGUN YET
//ADD WAY TO INCLUDE SYNONYMS IN CHORD EDITOR PROMPTS
namespace ChordProgressionGenerator
{
    class Program
    {
        static void Main()
        {
            ChordSymbolService chordService = new("data/chordSymbols.json");
            List<ChordSymbol> chords = chordService.LoadChords();

            ChordProgressionService progressionService = new("data/chordProgressions.json");
            List<ChordProgression> progressions = progressionService.LoadProgressions();

            ChordSymbolEditor chordEditor = new(chordService);

            ChordAnalysisService analysisService = new(chordService);
            ProgressionFilterService filterService = new();
            ProgressionBuilderService builderService = new(analysisService, filterService, chordService, chordEditor, progressionService);

            // === Canonical Lookup (Symbol -> Roman) ===
            Dictionary<string, string> canonicalLookup = chords.ToDictionary(
                chord => chord.Symbol,
                chord => chord.RomanNumeral ?? chord.Symbol
            );

            // === Test Your Code Below ===
            //CheckForMissingChords(chords, progressions, chordEditor, analysisService);
            TestBuildModulationV2("I", "vi", chords, progressions, builderService);
            //TestBackwardPath("Am", 6, chords, progressions, builderService);
            //TestDirectModPaths("vi", 8, chords, progressions, builderService);
            //TestCommonToneModulationPaths("bVII", 8, chords, progressions, builderService);
            //PrintSimilarChords("I", chords, builderService);
            //TestBuildModulationProgression("ii", "vi", chords, progressions, builderService);
            //TestBuildProgression(chords, progressions, builderService, chordEditor);
            //TestChordAnalysis(chords, progressions, analysisService);
        }

        /*============================TESTING MODULES===============================*/
        static void CheckForMissingChords(
            List<ChordSymbol> chords,
            List<ChordProgression> progressions, 
            ChordSymbolEditor chordEditor, 
            ChordAnalysisService analysisService)
        {
            Dictionary<string, string> canonicalLookup = analysisService.BuildCanonicalLookup(chords);
            foreach (var progression in progressions)
            {
                List<string> allChords = analysisService.FlattenAndNormalize(progression, canonicalLookup);
                chordEditor.PromptToAddMissingChords(allChords);
            }
        }

        static void TestBuildModulationV2(
            string start, 
            string target, 
            List<ChordSymbol> chords, 
            List<ChordProgression> progressions, 
            ProgressionBuilderService builderService)
        {
            List<List<string>> possibleModulations = builderService.BuildModulationV2(start, target, chords, progressions);
            foreach (List<string> modulation in possibleModulations)
            {
                Console.WriteLine(string.Join(", ", modulation));
            }
        }
        
        static void TestDirectModPaths(
            string target, 
            int length, 
            List<ChordSymbol> chords, 
            List<ChordProgression> progressions,
            ProgressionBuilderService builderService)
        {
            List<List<string>> possiblePaths = builderService.DirectModulationPaths(target, length, chords, progressions);
            foreach (var path in possiblePaths)
            {
                Console.WriteLine(string.Join(", ", path));
            }
        }

        static void TestCommonToneModulationPaths(
            string target, 
            int length, 
            List<ChordSymbol> chords, 
            List<ChordProgression> progressions, 
            ProgressionBuilderService builderService)
        {
            List<List<string>> possiblePaths = builderService.CommonToneModulationPaths(target, length, chords, progressions);
            foreach (var path in possiblePaths)
            {
                Console.WriteLine(string.Join(", ", path));
            }
        }

        static void PrintSimilarChords(string transitionChord, List<ChordSymbol> chords, ProgressionBuilderService builderService)
        {
            List<string> similarChords = builderService.FindSimilarChords(transitionChord, chords);
            /* List<string> similarChordsByRoman = new();

            foreach (var chord in similarChords)
                similarChordsByRoman.Add(chord.RomanNumeral);
 */
            Console.WriteLine(string.Join(", ", similarChords));
        }

        static void TestBackwardPath(
            string target, 
            int length, 
            List<ChordSymbol> chords, 
            List<ChordProgression> progressions,
            ProgressionBuilderService builderService)
        {
            Console.WriteLine($"\nGenerating backward path from: {target}, length: {length}");

            List<string> path = builderService.BuildBackwardPath(target, length, chords, progressions);

            if (path.Count == 0)
            {
                Console.WriteLine("⚠️  No valid backward path found.");
            }
            else
            {
                Console.WriteLine("✅ Backward path:");
                Console.WriteLine(string.Join(" → ", path));
            }
        }


        /* static void TestBuildModulationProgression(
            string firstChord, 
            string targetChord, 
            List<ChordSymbol> chords, 
            List<ChordProgression> progressions, 
            ProgressionBuilderService builderService)
        {
            List<string> newModulation = builderService.BuildModulationProgression(
                firstChord, targetChord, chords, progressions
            );

            Console.WriteLine($"\n=== MODULATION PROGRESSION ===");
            Console.WriteLine(string.Join(" → ", newModulation));
        } */

        static void TestBuildProgression(
            List<ChordSymbol> chords, 
            List<ChordProgression> progressions,
            ProgressionBuilderService builderService,
            ChordSymbolEditor chordEditor)
        {
            List<string> newProgression = builderService.BuildProgression(chords, progressions);

            Console.WriteLine($"\n=== STANDARD PROGRESSION ===");
            Console.WriteLine(string.Join(" → ", newProgression));
            chordEditor.PromptToAddMissingChords(newProgression);
        }

        static void TestChordAnalysis(
            List<ChordSymbol> chords, 
            List<ChordProgression> progressions,
            ChordAnalysisService analysisService)
        {
            ProgressionFilterService filterService = new();
            List<ChordProgression> filtered = filterService.GetUserFilters(progressions);

            /* Console.WriteLine("\n=== CHORD PAIR FREQUENCIES ===\n");
            Dictionary<ChordPair, double> pairPercentages = analysisService.GetChordPairFrequencies(chords, progressions);
            foreach (KeyValuePair<ChordPair, double> kvp in pairPercentages.OrderByDescending(kvp => kvp.Value))
            {
                Console.WriteLine($"{kvp.Key.From} → {kvp.Key.To} : {(kvp.Value * 100):F2}%");
            }

            Console.WriteLine("\n=== FIRST CHORD FREQUENCIES ===\n");
            Dictionary<string, double> chordPercentages = analysisService.GetFirstChordFrequencies(chords, filtered);
            foreach (KeyValuePair<string, double> kvp in chordPercentages.OrderByDescending(kvp => kvp.Value))
            {
                Console.WriteLine($"{kvp.Key} : {(kvp.Value * 100):F2}%");
            } */

            Console.WriteLine("\n=== TRANSITION MAPS ===\n");
            
            Console.WriteLine("\n--> Forwards -->\n");
            Dictionary<string, List<string>> forwardMap = analysisService.GetForwardTransitions(chords, filtered);
            foreach (var entry in forwardMap.OrderBy(kvp => kvp.Key))
            {
                var groupedTargets = entry.Value.GroupBy(x => x)
                                                .Select(g => $"{g.Key} ({g.Count()})")
                                                .OrderBy(s => s)
                                                .ToList();

                int totalTransitions = entry.Value.Count;
                Console.WriteLine($"{entry.Key} -> {string.Join(", ", groupedTargets)}, Total: {totalTransitions}");
            }

            Console.WriteLine("\n<-- Backwards <--\n");
            Dictionary<string, List<string>> backwardMap = analysisService.GetBackwardTransitions(chords, filtered);
            foreach (var entry in backwardMap.OrderBy(kvp => kvp.Key))
            {
                var groupedTargets = entry.Value.GroupBy(x => x)
                                                .Select(g => $"{g.Key} ({g.Count()})")
                                                .OrderBy(s => s)
                                                .ToList();

                int totalTransitions = entry.Value.Count;
                Console.WriteLine($"{entry.Key} <- {string.Join(", ", groupedTargets)}, Total: {totalTransitions}");
            }
        }
    }
}
