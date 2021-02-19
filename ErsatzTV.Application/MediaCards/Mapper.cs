using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.MediaCards
{
    internal static class Mapper
    {
        internal static TelevisionShowCardViewModel ProjectToViewModel(TelevisionShow televisionShow) =>
            new(
                televisionShow.Id,
                televisionShow.Metadata.Title,
                televisionShow.Metadata.Year.ToString(),
                televisionShow.Metadata.SortTitle,
                televisionShow.Poster);

        internal static TelevisionSeasonCardViewModel ProjectToViewModel(TelevisionSeason televisionSeason) =>
            new(
                televisionSeason.Id,
                GetSeasonName(televisionSeason.Number),
                string.Empty,
                GetSeasonName(televisionSeason.Number),
                televisionSeason.Poster,
                televisionSeason.Number == 0 ? "S" : televisionSeason.Number.ToString());

        internal static MovieCardViewModel ProjectToViewModel(MovieMediaItem movie) =>
            new(
                movie.Id,
                movie.Metadata.Title,
                movie.Metadata.Year.ToString(),
                movie.Metadata.SortTitle,
                movie.Poster);

        private static string GetSeasonName(int number) =>
            number == 0 ? "Specials" : $"Season {number}";
    }
}
