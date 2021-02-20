using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Television
{
    internal static class Mapper
    {
        internal static TelevisionShowViewModel ProjectToViewModel(TelevisionShow show) =>
            new(show.Metadata.Title, show.Metadata.Year?.ToString(), show.Metadata.Plot, show.Poster);

        internal static TelevisionSeasonViewModel ProjectToViewModel(TelevisionSeason season) =>
            new(
                season.TelevisionShowId,
                season.TelevisionShow.Metadata.Title,
                season.TelevisionShow.Metadata.Year?.ToString(),
                season.Number == 0 ? "Specials" : $"Season {season.Number}",
                season.Poster);

        internal static TelevisionEpisodeViewModel ProjectToViewModel(TelevisionEpisodeMediaItem episode) =>
            new(
                episode.Season.TelevisionShowId,
                episode.SeasonId,
                episode.Metadata.Episode,
                episode.Metadata.Title,
                episode.Metadata.Plot,
                episode.Poster);
    }
}
