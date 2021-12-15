using System.Linq;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Emby;
using ErsatzTV.Core.Extensions;
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
                GetPoster(showMetadata, maybeJellyfin, maybeEmby),
                showMetadata.Show.State);
        
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
                season.SeasonNumber == 0 ? "S" : season.SeasonNumber.ToString(),
                season.State);

        internal static TelevisionSeasonCardViewModel ProjectToViewModel(
            SeasonMetadata seasonMetadata,
            Option<JellyfinMediaSource> maybeJellyfin,
            Option<EmbyMediaSource> maybeEmby)
        {
            string showTitle = seasonMetadata.Season.Show.ShowMetadata.HeadOrNone().Match(
                m => m.Title ?? string.Empty,
                () => string.Empty);

            return new TelevisionSeasonCardViewModel(
                showTitle,
                seasonMetadata.SeasonId,
                seasonMetadata.Season.SeasonNumber,
                showTitle,
                GetSeasonName(seasonMetadata.Season.SeasonNumber),
                $"{showTitle}_{seasonMetadata.Season.SeasonNumber:0000}",
                GetPoster(seasonMetadata, maybeJellyfin, maybeEmby),
                seasonMetadata.Season.SeasonNumber == 0 ? "S" : seasonMetadata.Season.SeasonNumber.ToString(),
                seasonMetadata.Season.State);
        }

        internal static TelevisionEpisodeCardViewModel ProjectToViewModel(
            EpisodeMetadata episodeMetadata,
            Option<JellyfinMediaSource> maybeJellyfin,
            Option<EmbyMediaSource> maybeEmby,
            bool isSearchResult) =>
            new(
                episodeMetadata.EpisodeId,
                episodeMetadata.ReleaseDate ?? SystemTime.MinValueUtc,
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
                    ? GetEpisodePoster(episodeMetadata, maybeJellyfin, maybeEmby)
                    : GetThumbnail(episodeMetadata, maybeJellyfin, maybeEmby),
                episodeMetadata.Directors.Map(d => d.Name).ToList(),
                episodeMetadata.Writers.Map(w => w.Name).ToList(),
                episodeMetadata.Episode.State);

        internal static MovieCardViewModel ProjectToViewModel(
            MovieMetadata movieMetadata,
            Option<JellyfinMediaSource> maybeJellyfin,
            Option<EmbyMediaSource> maybeEmby) =>
            new(
                movieMetadata.MovieId,
                movieMetadata.Title,
                movieMetadata.Year?.ToString(),
                movieMetadata.SortTitle,
                GetPoster(movieMetadata, maybeJellyfin, maybeEmby),
                movieMetadata.Movie.State);

        internal static MusicVideoCardViewModel ProjectToViewModel(MusicVideoMetadata musicVideoMetadata) =>
            new(
                musicVideoMetadata.MusicVideoId,
                musicVideoMetadata.Title,
                musicVideoMetadata.MusicVideo.Artist.ArtistMetadata.Head().Title,
                musicVideoMetadata.SortTitle,
                musicVideoMetadata.Plot,
                musicVideoMetadata.Album,
                GetThumbnail(musicVideoMetadata, None, None),
                musicVideoMetadata.MusicVideo.State,
                musicVideoMetadata.MusicVideo.GetHeadVersion().MediaFiles.Head().Path);

        internal static OtherVideoCardViewModel ProjectToViewModel(OtherVideoMetadata otherVideoMetadata) =>
            new(
                otherVideoMetadata.OtherVideoId,
                otherVideoMetadata.Title,
                otherVideoMetadata.OriginalTitle,
                otherVideoMetadata.SortTitle,
                otherVideoMetadata.OtherVideo.State);

        internal static SongCardViewModel ProjectToViewModel(SongMetadata songMetadata)
        {
            string album = string.IsNullOrWhiteSpace(songMetadata.Album) ? "" : $" - {songMetadata.Album}";
            return new SongCardViewModel(
                songMetadata.SongId,
                songMetadata.Title,
                songMetadata.Artist + album,
                songMetadata.SortTitle,
                GetThumbnail(songMetadata, None, None),
                songMetadata.Song.State);
        }

        internal static ArtistCardViewModel ProjectToViewModel(ArtistMetadata artistMetadata) =>
            new(
                artistMetadata.ArtistId,
                artistMetadata.Title,
                artistMetadata.Disambiguation,
                artistMetadata.SortTitle,
                GetThumbnail(artistMetadata, None, None),
                artistMetadata.Artist.State);

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
                    .ToList(),
                collection.MediaItems.OfType<OtherVideo>().Map(ov => ProjectToViewModel(ov.OtherVideoMetadata.Head()))
                    .ToList(),
                collection.MediaItems.OfType<Song>().Map(s => ProjectToViewModel(s.SongMetadata.Head()))
                    .ToList()) { UseCustomPlaybackOrder = collection.UseCustomPlaybackOrder };

        internal static ActorCardViewModel ProjectToViewModel(
            Actor actor,
            Option<JellyfinMediaSource> maybeJellyfin,
            Option<EmbyMediaSource> maybeEmby)
        {
            string artwork = actor.Artwork?.Path ?? string.Empty;

            if (maybeJellyfin.IsSome && artwork.StartsWith("jellyfin://"))
            {
                artwork = JellyfinUrl.RelativeProxyForArtwork(artwork)
                    .SetQueryParam("fillHeight", 440);
            }
            else if (maybeEmby.IsSome && artwork.StartsWith("emby://"))
            {
                artwork = EmbyUrl.RelativeProxyForArtwork(artwork)
                    .SetQueryParam("maxHeight", 440);
            }

            return new ActorCardViewModel(actor.Id, actor.Name, actor.Role, artwork, MediaItemState.Normal);
        }

        private static int GetCustomIndex(Collection collection, int mediaItemId) =>
            Optional(collection.CollectionItems.Find(ci => ci.MediaItemId == mediaItemId))
                .Map(ci => ci.CustomIndex ?? 0)
                .IfNone(0);

        private static string GetSeasonName(int number) =>
            number == 0 ? "Specials" : $"Season {number}";

        private static string GetEpisodePoster(
            EpisodeMetadata episodeMetadata,
            Option<JellyfinMediaSource> maybeJellyfin,
            Option<EmbyMediaSource> maybeEmby)
        {
            Option<SeasonMetadata> maybeSeasonMetadata = episodeMetadata.Episode.Season.SeasonMetadata.HeadOrNone();
            return maybeSeasonMetadata.Match(
                seasonMetadata => GetPoster(seasonMetadata, maybeJellyfin, maybeEmby),
                () =>
                {
                    Option<ShowMetadata> maybeShowMetadata =
                        episodeMetadata.Episode.Season.Show.ShowMetadata.HeadOrNone();
                    return maybeShowMetadata.Match(
                        showMetadata => GetPoster(showMetadata, maybeJellyfin, maybeEmby),
                        () => string.Empty);
                });
        }

        private static string GetPoster(
            Metadata metadata,
            Option<JellyfinMediaSource> maybeJellyfin,
            Option<EmbyMediaSource> maybeEmby)
        {
            string poster = Optional(metadata.Artwork.FirstOrDefault(a => a.ArtworkKind == ArtworkKind.Poster))
                .Match(a => a.Path, string.Empty);

            if (maybeJellyfin.IsSome && poster.StartsWith("jellyfin://"))
            {
                poster = JellyfinUrl.RelativeProxyForArtwork(poster)
                    .SetQueryParam("fillHeight", 440);
            }
            else if (maybeEmby.IsSome && poster.StartsWith("emby://"))
            {
                poster = EmbyUrl.RelativeProxyForArtwork(poster)
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
                thumb = JellyfinUrl.RelativeProxyForArtwork(thumb)
                    .SetQueryParam("fillHeight", 220);
            }
            else if (maybeEmby.IsSome && thumb.StartsWith("emby://"))
            {
                thumb = EmbyUrl.RelativeProxyForArtwork(thumb)
                    .SetQueryParam("maxHeight", 220);
            }

            return thumb;
        }
    }
}
