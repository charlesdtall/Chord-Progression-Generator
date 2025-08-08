using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ChordProgressionGenerator.Models;

namespace ChordProgressionGenerator.Services
{
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

            string json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<ChordProgression>>(json) ?? new List<ChordProgression>();
        }

        public void SaveProgressions(List<ChordProgression> progressions)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(progressions, options);
            File.WriteAllText(_filePath, json);
        }
    }
}
