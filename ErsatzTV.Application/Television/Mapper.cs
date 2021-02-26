using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Television
{
    internal static class Mapper
    {
        internal static TelevisionShowViewModel ProjectToViewModel(Show show) =>
            new(
                show.Id,
                show.ShowMetadata.HeadOrNone().Map(m => m.Title).IfNone(string.Empty),
                show.ShowMetadata.HeadOrNone().Map(m => m.ReleaseDate?.Year.ToString()).IfNone(string.Empty),
                show.ShowMetadata.HeadOrNone().Map(m => m.Plot).IfNone(string.Empty),
                null); // TODO: artwork

        internal static TelevisionSeasonViewModel ProjectToViewModel(Season season) =>
            new(
                season.Id,
                season.ShowId,
                season.Show.ShowMetadata.HeadOrNone().Map(m => m.Title).IfNone(string.Empty),
                season.Show.ShowMetadata.HeadOrNone().Map(m => m.ReleaseDate?.Year.ToString()).IfNone(string.Empty),
                season.SeasonNumber == 0 ? "Specials" : $"Season {season.SeasonNumber}",
                null); // TODO: artwork

        internal static TelevisionEpisodeViewModel ProjectToViewModel(Episode episode) =>
            new(
                episode.Season.ShowId,
                episode.SeasonId,
                episode.EpisodeNumber,
                episode.EpisodeMetadata.HeadOrNone().Map(m => m.Title).IfNone(string.Empty),
                episode.EpisodeMetadata.HeadOrNone().Map(m => m.Plot).IfNone(string.Empty),
                null); // TODO: artwork
    }
}
