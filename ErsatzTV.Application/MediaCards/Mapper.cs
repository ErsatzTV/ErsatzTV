using System;
using System.Linq;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Emby;
using ErsatzTV.Core.Jellyfin;
using LanguageExt;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.MediaCards
{
    internal static class Mapper
    {
        internal static TelevisionShowCardViewModel ProjectToViewModel(
            ShowMetadata showMetadata,
            Option<JellyfinMediaSource> maybeJellyfin,
            Option<EmbyMediaSource> maybeEmby) =>
            new(
                showMetadata.ShowId,
                showMetadata.Title,
                showMetadata.Year?.ToString(),
                showMetadata.SortTitle,
                GetPoster(showMetadata, maybeJellyfin, maybeEmby));

        internal static TelevisionSeasonCardViewModel ProjectToViewModel(
            Season season,
            Option<JellyfinMediaSource> maybeJellyfin,
            Option<EmbyMediaSource> maybeEmby) =>
            new(
                season.Show.ShowMetadata.HeadOrNone().Match(m => m.Title ?? string.Empty, () => string.Empty),
                season.Id,
                season.SeasonNumber,
                GetSeasonName(season.SeasonNumber),
                string.Empty,
                GetSeasonName(season.SeasonNumber),
                season.SeasonMetadata.HeadOrNone().Map(sm => GetPoster(sm, maybeJellyfin, maybeEmby))
                    .IfNone(string.Empty),
                season.SeasonNumber == 0 ? "S" : season.SeasonNumber.ToString());

        internal static TelevisionEpisodeCardViewModel ProjectToViewModel(
            EpisodeMetadata episodeMetadata,
            Option<JellyfinMediaSource> maybeJellyfin,
            Option<EmbyMediaSource> maybeEmby,
            bool isSearchResult) =>
            new(
                episodeMetadata.EpisodeId,
                episodeMetadata.ReleaseDate ?? DateTime.MinValue,
                episodeMetadata.Episode.Season.Show.ShowMetadata.HeadOrNone().Match(
                    m => m.Title ?? string.Empty,
                    () => string.Empty),
                episodeMetadata.Episode.Season.ShowId,
                episodeMetadata.Episode.SeasonId,
                episodeMetadata.Episode.Season.SeasonNumber,
                episodeMetadata.Episode.EpisodeMetadata.HeadOrNone().Match(em => em.EpisodeNumber, () => 0),
                episodeMetadata.Title,
                episodeMetadata.SortTitle,
                episodeMetadata.Episode.EpisodeMetadata.HeadOrNone().Match(
                    em => em.Plot ?? string.Empty,
                    () => string.Empty),
                isSearchResult
                    ? GetPoster(
                        episodeMetadata.Episode.Season.SeasonMetadata.Head(),
                        maybeJellyfin,
                        maybeEmby)
                    : GetThumbnail(episodeMetadata, maybeJellyfin, maybeEmby),
                episodeMetadata.Directors.Map(d => d.Name).ToList(),
                episodeMetadata.Writers.Map(w => w.Name).ToList());

        internal static MovieCardViewModel ProjectToViewModel(
            MovieMetadata movieMetadata,
            Option<JellyfinMediaSource> maybeJellyfin,
            Option<EmbyMediaSource> maybeEmby) =>
            new(
                movieMetadata.MovieId,
                movieMetadata.Title,
                movieMetadata.Year?.ToString(),
                movieMetadata.SortTitle,
                GetPoster(movieMetadata, maybeJellyfin, maybeEmby));

        internal static MusicVideoCardViewModel ProjectToViewModel(MusicVideoMetadata musicVideoMetadata) =>
            new(
                musicVideoMetadata.MusicVideoId,
                musicVideoMetadata.Title,
                musicVideoMetadata.MusicVideo.Artist.ArtistMetadata.Head().Title,
                musicVideoMetadata.SortTitle,
                musicVideoMetadata.Plot,
                GetThumbnail(musicVideoMetadata, None, None));

        internal static ArtistCardViewModel ProjectToViewModel(ArtistMetadata artistMetadata) =>
            new(
                artistMetadata.ArtistId,
                artistMetadata.Title,
                artistMetadata.Disambiguation,
                artistMetadata.SortTitle,
                GetThumbnail(artistMetadata, None, None));

        internal static CollectionCardResultsViewModel
            ProjectToViewModel(
                Collection collection,
                Option<JellyfinMediaSource> maybeJellyfin,
                Option<EmbyMediaSource> maybeEmby) =>
            new(
                collection.Name,
                collection.MediaItems.OfType<Movie>().Map(
                    m => ProjectToViewModel(m.MovieMetadata.Head(), maybeJellyfin, maybeEmby) with
                    {
                        CustomIndex = GetCustomIndex(collection, m.Id)
                    }).ToList(),
                collection.MediaItems.OfType<Show>()
                    .Map(s => ProjectToViewModel(s.ShowMetadata.Head(), maybeJellyfin, maybeEmby))
                    .ToList(),
                collection.MediaItems.OfType<Season>().Map(s => ProjectToViewModel(s, maybeJellyfin, maybeEmby))
                    .ToList(),
                collection.MediaItems.OfType<Episode>()
                    .Map(e => ProjectToViewModel(e.EpisodeMetadata.Head(), maybeJellyfin, maybeEmby, false))
                    .ToList(),
                collection.MediaItems.OfType<Artist>().Map(a => ProjectToViewModel(a.ArtistMetadata.Head())).ToList(),
                collection.MediaItems.OfType<MusicVideo>().Map(mv => ProjectToViewModel(mv.MusicVideoMetadata.Head()))
                    .ToList()) { UseCustomPlaybackOrder = collection.UseCustomPlaybackOrder };

        internal static ActorCardViewModel ProjectToViewModel(
            Actor actor,
            Option<JellyfinMediaSource> maybeJellyfin,
            Option<EmbyMediaSource> maybeEmby)
        {
            string artwork = actor.Artwork?.Path ?? string.Empty;

            if (maybeJellyfin.IsSome && artwork.StartsWith("jellyfin://"))
            {
                artwork = JellyfinUrl.ForArtwork(maybeJellyfin, artwork)
                    .SetQueryParam("fillHeight", 440);
            }
            else if (maybeEmby.IsSome && artwork.StartsWith("emby://"))
            {
                artwork = EmbyUrl.ForArtwork(maybeEmby, artwork)
                    .SetQueryParam("maxHeight", 440);
            }

            return new ActorCardViewModel(actor.Id, actor.Name, actor.Role, artwork);
        }

        private static int GetCustomIndex(Collection collection, int mediaItemId) =>
            Optional(collection.CollectionItems.Find(ci => ci.MediaItemId == mediaItemId))
                .Map(ci => ci.CustomIndex ?? 0)
                .IfNone(0);

        private static string GetSeasonName(int number) =>
            number == 0 ? "Specials" : $"Season {number}";

        private static string GetPoster(
            Metadata metadata,
            Option<JellyfinMediaSource> maybeJellyfin,
            Option<EmbyMediaSource> maybeEmby)
        {
            string poster = Optional(metadata.Artwork.FirstOrDefault(a => a.ArtworkKind == ArtworkKind.Poster))
                .Match(a => a.Path, string.Empty);

            if (maybeJellyfin.IsSome && poster.StartsWith("jellyfin://"))
            {
                poster = JellyfinUrl.ForArtwork(maybeJellyfin, poster)
                    .SetQueryParam("fillHeight", 440);
            }
            else if (maybeEmby.IsSome && poster.StartsWith("emby://"))
            {
                poster = EmbyUrl.ForArtwork(maybeEmby, poster)
                    .SetQueryParam("maxHeight", 440);
            }

            return poster;
        }

        private static string GetThumbnail(
            Metadata metadata,
            Option<JellyfinMediaSource> maybeJellyfin,
            Option<EmbyMediaSource> maybeEmby)
        {
            string thumb = Optional(metadata.Artwork.FirstOrDefault(a => a.ArtworkKind == ArtworkKind.Thumbnail))
                .Match(a => a.Path, string.Empty);

            if (maybeJellyfin.IsSome && thumb.StartsWith("jellyfin://"))
            {
                thumb = JellyfinUrl.ForArtwork(maybeJellyfin, thumb)
                    .SetQueryParam("fillHeight", 220);
            }
            else if (maybeEmby.IsSome && thumb.StartsWith("emby://"))
            {
                thumb = EmbyUrl.ForArtwork(maybeEmby, thumb)
                    .SetQueryParam("maxHeight", 220);
            }

            return thumb;
        }
    }
}
