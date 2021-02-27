using System;
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
                showMetadata.ReleaseDate?.Year.ToString(),
                showMetadata.SortTitle,
                GetPoster(showMetadata));

        internal static TelevisionSeasonCardViewModel ProjectToViewModel(Season season) =>
            new(
                season.Show.ShowMetadata.HeadOrNone().Map(m => m.Title).IfNone(string.Empty),
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
                episodeMetadata.Episode.Season.Show.ShowMetadata.HeadOrNone().Map(m => m.Title).IfNone(string.Empty),
                episodeMetadata.Title,
                $"Episode {episodeMetadata.Episode.EpisodeNumber}",
                episodeMetadata.Episode.EpisodeNumber.ToString(),
                GetThumbnail(episodeMetadata),
                episodeMetadata.Episode.EpisodeNumber.ToString());

        internal static MovieCardViewModel ProjectToViewModel(MovieMetadata movieMetadata) =>
            new(
                movieMetadata.MovieId,
                movieMetadata.Title,
                movieMetadata.ReleaseDate?.Year.ToString(),
                movieMetadata.SortTitle,
                GetPoster(movieMetadata));

        internal static CollectionCardResultsViewModel
            ProjectToViewModel(Collection collection) =>
            new(
                collection.Name,
                collection.MediaItems.OfType<Movie>().Map(m => ProjectToViewModel(m.MovieMetadata.Head())).ToList(),
                collection.MediaItems.OfType<Show>().Map(s => ProjectToViewModel(s.ShowMetadata.Head())).ToList(),
                collection.MediaItems.OfType<Season>().Map(ProjectToViewModel).ToList(),
                collection.MediaItems.OfType<Episode>().Map(e => ProjectToViewModel(e.EpisodeMetadata.Head()))
                    .ToList());

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
