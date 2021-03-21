using System.Collections.Generic;
using System.Linq;
using ErsatzTV.Core.Domain;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.Television
{
    internal static class Mapper
    {
        internal static TelevisionShowViewModel ProjectToViewModel(Show show) =>
            new(
                show.Id,
                show.ShowMetadata.HeadOrNone().Map(m => m.Title ?? string.Empty).IfNone(string.Empty),
                show.ShowMetadata.HeadOrNone().Map(m => m.Year?.ToString() ?? string.Empty).IfNone(string.Empty),
                show.ShowMetadata.HeadOrNone().Map(m => m.Plot ?? string.Empty).IfNone(string.Empty),
                show.ShowMetadata.HeadOrNone().Map(GetPoster).IfNone(string.Empty),
                show.ShowMetadata.HeadOrNone().Map(GetFanArt).IfNone(string.Empty),
                show.ShowMetadata.HeadOrNone().Map(m => m.Genres.Map(g => g.Name).ToList()).IfNone(new List<string>()),
                show.ShowMetadata.HeadOrNone().Map(m => m.Tags.Map(g => g.Name).ToList()).IfNone(new List<string>()),
                show.ShowMetadata.HeadOrNone().Map(m => m.Studios.Map(s => s.Name).ToList())
                    .IfNone(new List<string>()));

        internal static TelevisionSeasonViewModel ProjectToViewModel(Season season) =>
            new(
                season.Id,
                season.ShowId,
                season.Show.ShowMetadata.HeadOrNone().Map(m => m.Title ?? string.Empty).IfNone(string.Empty),
                season.Show.ShowMetadata.HeadOrNone().Map(m => m.Year?.ToString() ?? string.Empty).IfNone(string.Empty),
                season.SeasonNumber == 0 ? "Specials" : $"Season {season.SeasonNumber}",
                season.SeasonMetadata.HeadOrNone().Map(GetPoster).IfNone(string.Empty),
                season.Show.ShowMetadata.HeadOrNone().Map(GetFanArt).IfNone(string.Empty));

        internal static TelevisionEpisodeViewModel ProjectToViewModel(Episode episode) =>
            new(
                episode.Season.ShowId,
                episode.SeasonId,
                episode.EpisodeNumber,
                episode.EpisodeMetadata.HeadOrNone().Map(m => m.Title ?? string.Empty).IfNone(string.Empty),
                episode.EpisodeMetadata.HeadOrNone().Map(m => m.Plot ?? string.Empty).IfNone(string.Empty),
                episode.EpisodeMetadata.HeadOrNone().Map(GetThumbnail).IfNone(string.Empty));

        private static string GetPoster(Metadata metadata) => GetArtwork(metadata, ArtworkKind.Poster);

        private static string GetFanArt(Metadata metadata) => GetArtwork(metadata, ArtworkKind.FanArt);

        private static string GetThumbnail(Metadata metadata) => GetArtwork(metadata, ArtworkKind.Thumbnail);

        private static string GetArtwork(Metadata metadata, ArtworkKind artworkKind) =>
            Optional(metadata.Artwork.FirstOrDefault(a => a.ArtworkKind == artworkKind))
                .Match(a => a.Path, string.Empty);
    }
}
