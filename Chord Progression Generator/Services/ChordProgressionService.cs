using System.Text.Json;
using ChordProgressionGenerator.Models;

namespace ChordProgressionGenerator.Services;

public class ChordProgressionService
{
    private readonly string _filePath;

    public ChordProgressionService(string filePath)
    {
        _filePath = filePath;
    }

    public List<ChordProgression> LoadProgressions()
    {
        if (!File.Exists(_filePath))
            return new List<ChordProgression>();

        var json = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize<List<ChordProgression>>(json) ?? new();
    }
}
