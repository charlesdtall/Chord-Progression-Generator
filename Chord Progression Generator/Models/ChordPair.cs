namespace ChordProgressionGenerator.Models
{
    public class ChordPair
    {
        public string From { get; }
        public string To { get; }

        public ChordPair(string from, string to)
        {
            From = from;
            To = to;
        }

        // Required for using ChordPair as a Dictionary key
        public override bool Equals(object? obj)
        {
            if (obj is not ChordPair other) return false;
            return From == other.From && To == other.To;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(From, To);
        }
    }
}
