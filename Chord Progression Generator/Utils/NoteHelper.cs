using System;
using System.Collections.Generic;

namespace ChordProgressionGenerator.Utils
{
    public static class NoteHelper
    {
        // Sharp and flat enharmonic note names
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

        // Map every possible name to pitch class
        private static readonly Dictionary<string, int> NameToPitchClass = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            // Sharps
            ["C"] = 0, ["C#"] = 1, ["D"] = 2, ["D#"] = 3, ["E"] = 4,
            ["F"] = 5, ["F#"] = 6, ["G"] = 7, ["G#"] = 8, ["A"] = 9,
            ["A#"] = 10, ["B"] = 11,

            // Flats
            ["Db"] = 1, ["Eb"] = 3, ["Gb"] = 6, ["Ab"] = 8, ["Bb"] = 10
        };

        // Converts a pitch class number (0–11) to a note name.
        public static string GetNoteName(int pitchClass, bool preferSharps = true)
        {
            int pc = ((pitchClass % 12) + 12) % 12;
            return preferSharps ? Sharps[pc] : Flats[pc];
        }

        // Converts a note name like "C#" or "Db" to its pitch class number (0–11).
        // Returns -1 if invalid.
        public static int GetPitchClass(string noteName)
        {
            return NameToPitchClass.TryGetValue(noteName.Trim(), out int value) ? value : -1;
        }

        // Returns both sharp and flat names for a pitch class.
        public static List<string> GetEnharmonicNames(int pitchClass)
        {
            int pc = ((pitchClass % 12) + 12) % 12;
            List<string> names = new List<string>();

            if (!names.Contains(Sharps[pc])) names.Add(Sharps[pc]);
            if (!names.Contains(Flats[pc]) && Flats[pc] != Sharps[pc]) names.Add(Flats[pc]);

            return names;
        }
    }
}
