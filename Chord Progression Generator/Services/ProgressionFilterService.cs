using System;
using System.Collections.Generic;
using System.Linq;
using ChordProgressionGenerator.Models;

namespace ChordProgressionGenerator.Services
{
    public class ProgressionFilterService
    {
        public List<ChordProgression> FilterProgressions(
            List<ChordProgression> progressions,
            List<string>? filterGenres,
            string? period,
            string? composer,
            string? artist,
            string? type,
            int? yearAfter,
            int? yearBefore)
        {
            return progressions
                .Where(p =>
                    (filterGenres == null || (p.Genre != null && p.Genre.Any(g => filterGenres.Any(fg => fg.Equals(g, StringComparison.OrdinalIgnoreCase))))) &&
                    (string.IsNullOrEmpty(period) || p.Period?.Equals(period, StringComparison.OrdinalIgnoreCase) == true) &&
                    (string.IsNullOrEmpty(composer) || p.Composer?.Equals(composer, StringComparison.OrdinalIgnoreCase) == true) &&
                    (string.IsNullOrEmpty(artist) || p.Artist?.Equals(artist, StringComparison.OrdinalIgnoreCase) == true) &&
                    (string.IsNullOrEmpty(type) || p.Type?.Equals(type, StringComparison.OrdinalIgnoreCase) == true) &&
                    (!yearAfter.HasValue || (p.Year.HasValue && p.Year.Value >= yearAfter.Value)) &&
                    (!yearBefore.HasValue || (p.Year.HasValue && p.Year.Value <= yearBefore.Value))
                )
                .ToList();
        }

        public List<ChordProgression> GetUserFilters(List<ChordProgression> progressions)
        {
            Console.Write("Do you want to apply filters? (y/n): ");
            string? response = Console.ReadLine()?.Trim().ToLower();

            List<string>? filterGenres = null;
            string? period = null;
            string? artist = null;
            string? composer = null;
            string? type = null;
            int? yearAfter = null;
            int? yearBefore = null;

            if (response == "y")
            {
                Console.Write("Filter by Genre(s) (comma separated, or leave blank): ");
                string? genreInput = Console.ReadLine()?.Trim();
                if (!string.IsNullOrEmpty(genreInput))
                {
                    filterGenres = genreInput
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(g => g.Trim())
                        .ToList();
                }

                Console.Write("Filter by Period (or leave blank): ");
                period = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(period)) period = null;

                Console.Write("Filter by Artist (or leave blank): ");
                artist = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(artist)) artist = null;

                Console.Write("Filter by Composer (or leave blank): ");
                composer = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(composer)) composer = null;

                Console.Write("Filter by Type (or leave blank): ");
                type = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(type)) type = null;

                Console.Write("Filter by minimum Year (or leave blank): ");
                string? yearAfterInput = Console.ReadLine()?.Trim();
                yearAfter = int.TryParse(yearAfterInput, out int ya) ? ya : null;

                Console.Write("Filter by maximum Year (or leave blank): ");
                string? yearBeforeInput = Console.ReadLine()?.Trim();
                yearBefore = int.TryParse(yearBeforeInput, out int yb) ? yb : null;
            }

            List<ChordProgression> filtered = FilterProgressions(
                progressions, filterGenres, period, composer, artist, type, yearAfter, yearBefore
            );

            return filtered;
        }
    }
}
