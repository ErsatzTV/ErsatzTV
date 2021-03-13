using System;
using System.Collections.Generic;
using System.Linq;
using ErsatzTV.Core.Domain;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.MediaCards
{
    internal static class Mapper
    {
        internal static TelevisionShowCardViewModel ProjectToViewModel(ShowMetadata showMetadata) =>
            new(
                showMetadata.ShowId,
                showMetadata.Title,
                showMetadata.Year?.ToString(),
                showMetadata.SortTitle,
                GetPoster(showMetadata));

        internal static TelevisionSeasonCardViewModel ProjectToViewModel(Season season) =>
            new(
                season.Show.ShowMetadata.HeadOrNone().Match(m => m.Title ?? string.Empty, () => string.Empty),
                season.Id,
                season.SeasonNumber,
                GetSeasonName(season.SeasonNumber),
                string.Empty,
                GetSeasonName(season.SeasonNumber),
                season.SeasonMetadata.HeadOrNone().Map(GetPoster).IfNone(string.Empty),
                season.SeasonNumber == 0 ? "S" : season.SeasonNumber.ToString());

        internal static TelevisionEpisodeCardViewModel ProjectToViewModel(
            EpisodeMetadata episodeMetadata) =>
            new(
                episodeMetadata.EpisodeId,
                episodeMetadata.ReleaseDate ?? DateTime.MinValue,
                episodeMetadata.Episode.Season.Show.ShowMetadata.HeadOrNone().Match(
                    m => m.Title ?? string.Empty,
                    () => string.Empty),
                episodeMetadata.Episode.Season.ShowId,
                episodeMetadata.Episode.SeasonId,
                episodeMetadata.Episode.EpisodeNumber,
                episodeMetadata.Title,
                episodeMetadata.Episode.EpisodeMetadata.HeadOrNone().Match(
                    em => em.Plot ?? string.Empty,
                    () => string.Empty),
                GetThumbnail(episodeMetadata));

        internal static MovieCardViewModel ProjectToViewModel(MovieMetadata movieMetadata) =>
            new(
                movieMetadata.MovieId,
                movieMetadata.Title,
                movieMetadata.Year?.ToString(),
                movieMetadata.SortTitle,
                GetPoster(movieMetadata));

        internal static CollectionCardResultsViewModel
            ProjectToViewModel(Collection collection) =>
            new(
                collection.Name,
                collection.MediaItems.OfType<Movie>().Map(
                    m => ProjectToViewModel(m.MovieMetadata.Head()) with
                    {
                        CustomIndex = GetCustomIndex(collection, m.Id)
                    }).ToList(),
                collection.MediaItems.OfType<Show>().Map(s => ProjectToViewModel(s.ShowMetadata.Head())).ToList(),
                collection.MediaItems.OfType<Season>().Map(ProjectToViewModel).ToList(),
                collection.MediaItems.OfType<Episode>().Map(e => ProjectToViewModel(e.EpisodeMetadata.Head()))
                    .ToList()) { UseCustomPlaybackOrder = collection.UseCustomPlaybackOrder };

        private static int GetCustomIndex(Collection collection, int mediaItemId) =>
            Optional(collection.CollectionItems.Find(ci => ci.MediaItemId == mediaItemId))
                .Map(ci => ci.CustomIndex ?? 0)
                .IfNone(0);

        internal static SearchCardResultsViewModel ProjectToSearchResults(List<MediaItem> items) =>
            new(
                items.OfType<Movie>().Map(m => ProjectToViewModel(m.MovieMetadata.Head())).ToList(),
                items.OfType<Show>().Map(s => ProjectToViewModel(s.ShowMetadata.Head())).ToList());

        private static string GetSeasonName(int number) =>
            number == 0 ? "Specials" : $"Season {number}";

        private static string GetPoster(Metadata metadata) =>
            Optional(metadata.Artwork.FirstOrDefault(a => a.ArtworkKind == ArtworkKind.Poster))
                .Match(a => a.Path, string.Empty);

        private static string GetThumbnail(Metadata metadata) =>
            Optional(metadata.Artwork.FirstOrDefault(a => a.ArtworkKind == ArtworkKind.Thumbnail))
                .Match(a => a.Path, string.Empty);
    }
}
