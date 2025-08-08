using ChordProgressionGenerator.Models;
using System.Text.Json;

namespace ChordProgressionGenerator.Services
{
    public class ChordSymbolService
    {
        private readonly string _filePath;
        private List<ChordSymbol> _chordSymbols = new();


        public ChordSymbolService(string filePath)
        {
            _filePath = filePath;
            _chordSymbols = LoadChords();
        }

        public List<ChordSymbol> LoadChords()
        {
            if (!File.Exists(_filePath))
            {
                Console.WriteLine($"⚠️ Warning: File not found at {_filePath}. Returning empty chord list.");
                return new List<ChordSymbol>();
            }

            string json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<ChordSymbol>>(json) ?? new List<ChordSymbol>();
        }

        public void SaveChords(List<ChordSymbol> chords)
        {
            string json = JsonSerializer.Serialize(chords, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }

        public ChordSymbol? FindByName(string input)
        {
            string normalized = input.Trim();

            return _chordSymbols.FirstOrDefault(cs =>
                string.Equals(cs.Symbol, normalized, StringComparison.OrdinalIgnoreCase) ||
                (cs.Synonyms != null && cs.Synonyms.Any(s => string.Equals(s, normalized, StringComparison.OrdinalIgnoreCase))) ||
                string.Equals(cs.RomanNumeral, normalized, StringComparison.Ordinal)
            );
        }

    }
}
