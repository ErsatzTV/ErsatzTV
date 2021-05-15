using System;
using System.Linq;
using ErsatzTV.Core.Domain;
using LanguageExt;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.MediaCards
{
    internal static class Mapper
    {
        internal static TelevisionShowCardViewModel ProjectToViewModel(
            ShowMetadata showMetadata,
            Option<JellyfinMediaSource> maybeJellyfin) =>
            new(
                showMetadata.ShowId,
                showMetadata.Title,
                showMetadata.Year?.ToString(),
                showMetadata.SortTitle,
                GetPoster(showMetadata, maybeJellyfin));

        internal static TelevisionSeasonCardViewModel ProjectToViewModel(Season season) =>
            new(
                season.Show.ShowMetadata.HeadOrNone().Match(m => m.Title ?? string.Empty, () => string.Empty),
                season.Id,
                season.SeasonNumber,
                GetSeasonName(season.SeasonNumber),
                string.Empty,
                GetSeasonName(season.SeasonNumber),
                season.SeasonMetadata.HeadOrNone().Map(sm => GetPoster(sm, None)).IfNone(string.Empty),
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

        internal static MovieCardViewModel ProjectToViewModel(
            MovieMetadata movieMetadata,
            Option<JellyfinMediaSource> maybeJellyfin) =>
            new(
                movieMetadata.MovieId,
                movieMetadata.Title,
                movieMetadata.Year?.ToString(),
                movieMetadata.SortTitle,
                GetPoster(movieMetadata, maybeJellyfin));

        internal static MusicVideoCardViewModel ProjectToViewModel(MusicVideoMetadata musicVideoMetadata) =>
            new(
                musicVideoMetadata.MusicVideoId,
                musicVideoMetadata.Title,
                musicVideoMetadata.MusicVideo.Artist.ArtistMetadata.Head().Title,
                musicVideoMetadata.SortTitle,
                musicVideoMetadata.Plot,
                GetThumbnail(musicVideoMetadata));

        internal static ArtistCardViewModel ProjectToViewModel(ArtistMetadata artistMetadata) =>
            new(
                artistMetadata.ArtistId,
                artistMetadata.Title,
                artistMetadata.Disambiguation,
                artistMetadata.SortTitle,
                GetThumbnail(artistMetadata));

        internal static CollectionCardResultsViewModel
            ProjectToViewModel(Collection collection, Option<JellyfinMediaSource> maybeJellyfin) =>
            new(
                collection.Name,
                collection.MediaItems.OfType<Movie>().Map(
                    m => ProjectToViewModel(m.MovieMetadata.Head(), maybeJellyfin) with
                    {
                        CustomIndex = GetCustomIndex(collection, m.Id)
                    }).ToList(),
                collection.MediaItems.OfType<Show>().Map(s => ProjectToViewModel(s.ShowMetadata.Head(), maybeJellyfin)).ToList(),
                collection.MediaItems.OfType<Season>().Map(ProjectToViewModel).ToList(),
                collection.MediaItems.OfType<Episode>().Map(e => ProjectToViewModel(e.EpisodeMetadata.Head()))
                    .ToList(),
                collection.MediaItems.OfType<Artist>().Map(a => ProjectToViewModel(a.ArtistMetadata.Head())).ToList(),
                collection.MediaItems.OfType<MusicVideo>().Map(mv => ProjectToViewModel(mv.MusicVideoMetadata.Head()))
                    .ToList()) { UseCustomPlaybackOrder = collection.UseCustomPlaybackOrder };

        internal static ActorCardViewModel ProjectToViewModel(Actor actor, Option<JellyfinMediaSource> maybeJellyfin)
        {
            string artwork = actor.Artwork?.Path ?? string.Empty;

            if (maybeJellyfin.IsSome && artwork.StartsWith("jellyfin://"))
            {
                string address = maybeJellyfin.Map(ms => ms.Connections.HeadOrNone().Map(c => c.Address))
                    .Flatten()
                    .IfNone("jellyfin://");
                artwork = artwork.Replace("jellyfin://", address) + "&fillheight=440";
            }

            return new ActorCardViewModel(actor.Id, actor.Name, actor.Role, artwork);
        }

        private static int GetCustomIndex(Collection collection, int mediaItemId) =>
            Optional(collection.CollectionItems.Find(ci => ci.MediaItemId == mediaItemId))
                .Map(ci => ci.CustomIndex ?? 0)
                .IfNone(0);

        private static string GetSeasonName(int number) =>
            number == 0 ? "Specials" : $"Season {number}";

        private static string GetPoster(Metadata metadata, Option<JellyfinMediaSource> maybeJellyfin)
        {
            string poster = Optional(metadata.Artwork.FirstOrDefault(a => a.ArtworkKind == ArtworkKind.Poster))
                .Match(a => a.Path, string.Empty);

            if (maybeJellyfin.IsSome && poster.StartsWith("jellyfin://"))
            {
                string address = maybeJellyfin.Map(ms => ms.Connections.HeadOrNone().Map(c => c.Address))
                    .Flatten()
                    .IfNone("jellyfin://");
                poster = poster.Replace("jellyfin://", address) + "&fillHeight=440";
            }

            return poster;
        }

        private static string GetThumbnail(Metadata metadata) =>
            Optional(metadata.Artwork.FirstOrDefault(a => a.ArtworkKind == ArtworkKind.Thumbnail))
                .Match(a => a.Path, string.Empty);
    }
}
