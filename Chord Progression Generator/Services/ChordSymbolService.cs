using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ChordProgressionGenerator.Models;

namespace ChordProgressionGenerator.Services;

public class ChordSymbolService
{
    private readonly string _filePath;

    public ChordSymbolService(string filePath)
    {
        _filePath = filePath;
    }

    public List<ChordSymbol> LoadChords()
    {
        if (!File.Exists(_filePath))
            return new List<ChordSymbol>();

        string json = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize<List<ChordSymbol>>(json) ?? new List<ChordSymbol>();
    }

    public void SaveChords(List<ChordSymbol> chords)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(chords, options);
        File.WriteAllText(_filePath, json);
    }
}
