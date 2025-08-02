using System;
using System.Collections.Generic;
using System.Linq;
using ChordProgressionGenerator.Utils;

namespace ChordProgressionGenerator.Utils;

public static class ScaleHelper
{
    // Define common scale patterns as intervals in semitones
    public static readonly Dictionary<string, int[]> ScaleFormulas = new()
    {
        { "Major",        new[] { 2, 2, 1, 2, 2, 2, 1 } },   // Ionian
        { "NaturalMinor", new[] { 2, 1, 2, 2, 1, 2, 2 } },   // Aeolian
        { "HarmonicMinor",new[] { 2, 1, 2, 2, 1, 3, 1 } },
        { "MelodicMinor", new[] { 2, 1, 2, 2, 2, 2, 1 } },
        { "Dorian",       new[] { 2, 1, 2, 2, 2, 1, 2 } },
        { "Phrygian",     new[] { 1, 2, 2, 2, 1, 2, 2 } },
        { "Lydian",       new[] { 2, 2, 2, 1, 2, 2, 1 } },
        { "Mixolydian",   new[] { 2, 2, 1, 2, 2, 1, 2 } },
        { "Locrian",      new[] { 1, 2, 2, 1, 2, 2, 2 } },
        { "PentatonicMajor", new[] { 2, 2, 3, 2, 3 } },
        { "PentatonicMinor", new[] { 3, 2, 2, 3, 2 } },
        { "Blues",        new[] { 3, 2, 1, 1, 3, 2 } }
    };


    // Generates a scale from a root note name and scale type.
    public static List<string> GetScale(string rootNote, string scaleType, bool useFlats = false)
    {
        Dictionary<string, int> noteMap = useFlats ? NoteHelper.FlatNoteToInt : NoteHelper.SharpNoteToInt;

        if (!noteMap.TryGetValue(rootNote, out int rootValue))
            throw new ArgumentException($"Invalid root note: {rootNote}");

        if (!ScaleFormulas.TryGetValue(scaleType, out int[] intervals))
            throw new ArgumentException($"Unknown scale type: {scaleType}");

        List<string> scale = new();
        int current = rootValue;
        scale.Add(NoteHelper.IntToNote(current, useFlats));

        foreach (int step in intervals)
        {
            current = (current + step) % 12;
            scale.Add(NoteHelper.IntToNote(current, useFlats));
        }

        return scale;
    }

    // Compares scales against eachother, regardless of enharmonics
    public static int CountCommonPitches(List<string> scale1, List<string> scale2)
    {
        HashSet<int> notes1 = new();
        HashSet<int> notes2 = new();

        foreach (string note in scale1)
            notes1.Add(NoteHelper.NoteToInt(note));

        foreach (string note in scale2)
            notes2.Add(NoteHelper.NoteToInt(note));

        notes1.IntersectWith(notes2);
        return notes1.Count;
    }

}
