using System;
using System.Collections.Generic;

namespace ChordProgressionGenerator.Utils
{
    public static class NoteHelper
    {
        private static readonly string[] Sharps = 
        {
            "C", "C#", "D", "D#", "E", "F", 
            "F#", "G", "G#", "A", "A#", "B"
        };

        private static readonly string[] Flats = 
        {
            "C", "Db", "D", "Eb", "E", "F", 
            "Gb", "G", "Ab", "A", "Bb", "B"
        };

        // Maps all known note names (sharps + flats) to pitch classes (0–11)
        private static readonly Dictionary<string, int> NameToPitchClass = new(StringComparer.OrdinalIgnoreCase)
        {
            // Sharps
            ["C"] = 0, ["C#"] = 1, ["D"] = 2, ["D#"] = 3, ["E"] = 4,
            ["F"] = 5, ["F#"] = 6, ["G"] = 7, ["G#"] = 8, ["A"] = 9,
            ["A#"] = 10, ["B"] = 11,

            // Flats
            ["Db"] = 1, ["Eb"] = 3, ["Gb"] = 6, ["Ab"] = 8, ["Bb"] = 10
        };

        // Converts a pitch class (0–11) to a note name (sharp or flat based on preference)
        public static string IntToNote(int pitchClass, bool useFlats = false)
        {
            int normalized = ((pitchClass % 12) + 12) % 12;
            return useFlats ? Flats[normalized] : Sharps[normalized];
        }

        // Converts a note name to pitch class, using any known form
        public static int NoteToInt(string noteName)
        {
            if (string.IsNullOrWhiteSpace(noteName))
                return -1;

            return GetPitchClass(noteName);
        }

        // Specifically for flat-style notes (e.g., "Db"). Returns -1 if invalid.
        public static int FlatNoteToInt(string noteName)
        {
            if (string.IsNullOrWhiteSpace(noteName))
                return -1;

            return NameToPitchClass.TryGetValue(noteName.Trim(), out int value) ? value : -1;
        }

        // Specifically for sharp-style notes (e.g., "C#"). Returns -1 if invalid.
        public static int SharpNoteToInt(string noteName)
        {
            if (string.IsNullOrWhiteSpace(noteName))
                return -1;

            return NameToPitchClass.TryGetValue(noteName.Trim(), out int value) ? value : -1;
        }

        // Internal general-purpose method used by NoteToInt
        private static int GetPitchClass(string noteName)
        {
            return NameToPitchClass.TryGetValue(noteName.Trim(), out int value) ? value : -1;
        }

        // Returns both enharmonic names (sharp + flat) for a given pitch class
        public static List<string> GetEnharmonicNames(int pitchClass)
        {
            int normalized = ((pitchClass % 12) + 12) % 12;
            List<string> names = new();

            string sharp = Sharps[normalized];
            string flat = Flats[normalized];

            names.Add(sharp);
            if (!names.Contains(flat) && flat != sharp)
                names.Add(flat);

            return names;
        }
    }
}
