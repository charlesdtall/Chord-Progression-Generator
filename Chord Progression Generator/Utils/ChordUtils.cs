using System;
using System.Collections.Generic;
using System.Linq;
using ChordProgressionGenerator.Utils;


namespace ChordProgressionGenerator.Utils
{
    public static class ChordUtils
    {
        private static readonly Dictionary<string, int> NoteSemitones = new()
        {
            { "C", 0 }, { "B#", 0 },
            { "C#", 1 }, { "Db", 1 },
            { "D", 2 },
            { "D#", 3 }, { "Eb", 3 },
            { "E", 4 }, { "Fb", 4 },
            { "F", 5 }, { "E#", 5 },
            { "F#", 6 }, { "Gb", 6 },
            { "G", 7 },
            { "G#", 8 }, { "Ab", 8 },
            { "A", 9 },
            { "A#", 10 }, { "Bb", 10 },
            { "B", 11 }, { "Cb", 11 }
        };

        public static int NoteToSemitone(string note)
        {
            if (NoteSemitones.TryGetValue(note, out int semitone))
                return semitone;

            throw new ArgumentException($"Unknown note: {note}");
        }

        public static int RootProximity(string chord1, string chord2)
        {
            string root1 = GetRoot(chord1);
            string root2 = GetRoot(chord2);

            int semitone1 = NoteToSemitone(root1);
            int semitone2 = NoteToSemitone(root2);

            int distance = Math.Abs(semitone1 - semitone2);
            return Math.Min(distance, 12 - distance); // Circular distance in 12-tone system
        }

        public static string GetRoot(string chordRoman)
        {
            chordRoman = GetSecondaryChordRoot(chordRoman);

            // Extract the root note (e.g., "bIII7" → "Eb")
            if (chordRoman.StartsWith("b"))
            {
                string numeral = chordRoman.Substring(1);
                return RomanToNote("b" + numeral);
            }
            else if (chordRoman.StartsWith("#"))
            {
                string numeral = chordRoman.Substring(1);
                return RomanToNote("#" + numeral);
            }
            else
            {
                return RomanToNote(chordRoman);
            }
        }

        // Assumes major key for mapping, just for proximity purposes
        public static string RomanToNote(string roman)
        {
            Dictionary<string, string> romanMap = new()
            {
                { "I", "C" },
                { "ii", "D" }, { "II", "D" },
                { "iii", "E" }, { "III", "E" },
                { "IV", "F" },
                { "V", "G" },
                { "vi", "A" }, { "VI", "A" },
                { "vii", "B" }, { "VII", "B" },
                { "bII", "Db" }, { "#I", "C#" },
                { "bIII", "Eb" }, { "#II", "D#" },
                { "bV", "Gb" }, { "#IV", "F#" },
                { "bVI", "Ab" }, { "#V", "G#" },
                { "bVII", "Bb" }, { "#VI", "A#" }
            };

            string cleanRoman = roman.Replace("7", "").Replace("6", "").Replace("64", "");
            return romanMap.ContainsKey(cleanRoman) ? romanMap[cleanRoman] : "C"; // Default fallback
        }

        public static bool AreEnharmonicallyEquivalent(string note1, string note2)
        {
            return NoteToSemitone(note1) == NoteToSemitone(note2);
        }

        public static bool AreChordsEnharmonicallyClose(string chord1, string chord2)
        {
            string root1 = GetRoot(chord1);
            string root2 = GetRoot(chord2);
            return AreEnharmonicallyEquivalent(root1, root2);
        }

        public static List<string> SortChordsByProximity(string targetChord, IEnumerable<string> candidates)
        {
            return candidates
                .OrderBy(c => RootProximity(c, targetChord))
                .ThenBy(c => c.Length)
                .ToList();
        }

        public static string GetSecondaryChordRoot(string chordRoman)
        {
            if (!chordRoman.Contains("/")) return chordRoman;

            string[] parts = chordRoman.Split('/');
            if (parts.Length != 2) return chordRoman;

            string secondaryFunction = parts[0]; // e.g., "V"
            string secondaryTargetRoman = parts[1]; // e.g., "V"

            // Step 1: Get the root note of the target Roman numeral
            string targetRootNote = RomanToNote(secondaryTargetRoman); // e.g., "V" → "G"
            if (targetRootNote == null) return chordRoman;

            int targetPitch = NoteHelper.NoteToInt(targetRootNote);

            // Step 2: Transpose based on function
            int resolvedPitch = secondaryFunction switch
            {
                "V" => (targetPitch + 7) % 12,        // Dominant: up a 5th
                "IV" => (targetPitch + 5) % 12,       // Subdominant: up a 4th
                "ii" => (targetPitch + 2) % 12,       // Minor supertonic
                "viio" => (targetPitch - 1 + 12) % 12,// Leading tone: down a ½ step
                _ => targetPitch
            };

            string resolvedNote = NoteHelper.IntToNote(resolvedPitch);
            
/*             // Step 3: Optionally apply chord quality
            string resolvedChord = secondaryFunction switch
            {
                "viio" => $"{resolvedNote}o",
                "ii" => $"{resolvedNote}m",
                _ => resolvedNote // Assume major
            }; */

            return resolvedNote;
        }
    }
}
