using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ErsatzTV.Application.MediaCards;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Emby;
using ErsatzTV.Core.Jellyfin;
using Flurl;
using LanguageExt;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.Television
{
    internal static class Mapper
    {
        internal static TelevisionShowViewModel ProjectToViewModel(
            Show show,
            List<string> languages,
            Option<JellyfinMediaSource> maybeJellyfin,
            Option<EmbyMediaSource> maybeEmby) =>
            new(
                show.Id,
                show.ShowMetadata.HeadOrNone().Map(m => m.Title ?? string.Empty).IfNone(string.Empty),
                show.ShowMetadata.HeadOrNone().Map(m => m.Year?.ToString() ?? string.Empty).IfNone(string.Empty),
                show.ShowMetadata.HeadOrNone().Map(m => m.Plot ?? string.Empty).IfNone(string.Empty),
                show.ShowMetadata.HeadOrNone().Map(m => GetPoster(m, maybeJellyfin, maybeEmby)).IfNone(string.Empty),
                show.ShowMetadata.HeadOrNone().Map(m => GetFanArt(m, maybeJellyfin, maybeEmby)).IfNone(string.Empty),
                show.ShowMetadata.HeadOrNone().Map(m => m.Genres.Map(g => g.Name).ToList()).IfNone(new List<string>()),
                show.ShowMetadata.HeadOrNone().Map(m => m.Tags.Map(g => g.Name).ToList()).IfNone(new List<string>()),
                show.ShowMetadata.HeadOrNone().Map(m => m.Studios.Map(s => s.Name).ToList())
                    .IfNone(new List<string>()),
                show.ShowMetadata.HeadOrNone()
                    .Map(
                        m => (m.ContentRating ?? string.Empty).Split("/").Map(s => s.Trim())
                            .Where(x => !string.IsNullOrWhiteSpace(x)).ToList()).IfNone(new List<string>()),
                LanguagesForShow(languages),
                show.ShowMetadata.HeadOrNone()
                    .Map(
                        m => m.Actors.OrderBy(a => a.Order).ThenBy(a => a.Id)
                            .Map(a => MediaCards.Mapper.ProjectToViewModel(a, maybeJellyfin, maybeEmby))
                            .ToList())
                    .IfNone(new List<ActorCardViewModel>()));

        internal static TelevisionSeasonViewModel ProjectToViewModel(
            Season season,
            Option<JellyfinMediaSource> maybeJellyfin,
            Option<EmbyMediaSource> maybeEmby) =>
            new(
                season.Id,
                season.ShowId,
                season.Show.ShowMetadata.HeadOrNone().Map(m => m.Title ?? string.Empty).IfNone(string.Empty),
                season.Show.ShowMetadata.HeadOrNone().Map(m => m.Year?.ToString() ?? string.Empty).IfNone(string.Empty),
                season.SeasonNumber == 0 ? "Specials" : $"Season {season.SeasonNumber}",
                season.SeasonMetadata.HeadOrNone().Map(m => GetPoster(m, maybeJellyfin, maybeEmby))
                    .IfNone(string.Empty),
                season.Show.ShowMetadata.HeadOrNone().Map(m => GetFanArt(m, maybeJellyfin, maybeEmby))
                    .IfNone(string.Empty));

        internal static TelevisionEpisodeViewModel ProjectToViewModel(Episode episode) =>
            new(
                episode.Season.ShowId,
                episode.SeasonId,
                episode.EpisodeNumber,
                episode.EpisodeMetadata.HeadOrNone().Map(m => m.Title ?? string.Empty).IfNone(string.Empty),
                episode.EpisodeMetadata.HeadOrNone().Map(m => m.Plot ?? string.Empty).IfNone(string.Empty),
                episode.EpisodeMetadata.HeadOrNone().Map(m => GetThumbnail(m, None, None)).IfNone(string.Empty));

        private static string GetPoster(
            Metadata metadata,
            Option<JellyfinMediaSource> maybeJellyfin,
            Option<EmbyMediaSource> maybeEmby) =>
            GetArtwork(metadata, ArtworkKind.Poster, maybeJellyfin, maybeEmby);

        private static string GetFanArt(
            Metadata metadata,
            Option<JellyfinMediaSource> maybeJellyfin,
            Option<EmbyMediaSource> maybeEmby) =>
            GetArtwork(metadata, ArtworkKind.FanArt, maybeJellyfin, maybeEmby);

        private static string GetThumbnail(
            Metadata metadata,
            Option<JellyfinMediaSource> maybeJellyfin,
            Option<EmbyMediaSource> maybeEmby) =>
            GetArtwork(metadata, ArtworkKind.Thumbnail, maybeJellyfin, maybeEmby);

        private static string GetArtwork(
            Metadata metadata,
            ArtworkKind artworkKind,
            Option<JellyfinMediaSource> maybeJellyfin,
            Option<EmbyMediaSource> maybeEmby)
        {
            string artwork = Optional(metadata.Artwork.FirstOrDefault(a => a.ArtworkKind == artworkKind))
                .Match(a => a.Path, string.Empty);

            if (maybeJellyfin.IsSome && artwork.StartsWith("jellyfin://"))
            {
                Url url = JellyfinUrl.ForArtwork(maybeJellyfin, artwork);
                if (artworkKind == ArtworkKind.Poster)
                {
                    url.SetQueryParam("fillHeight", 440);
                }

                artwork = url;
            }
            else if (maybeEmby.IsSome && artwork.StartsWith("emby://"))
            {
                Url url = EmbyUrl.ForArtwork(maybeEmby, artwork);
                if (artworkKind == ArtworkKind.Poster)
                {
                    url.SetQueryParam("maxHeight", 440);
                }

                artwork = url;
            }

            return artwork;
        }

        private static List<CultureInfo> LanguagesForShow(List<string> languages)
        {
            CultureInfo[] allCultures = CultureInfo.GetCultures(CultureTypes.NeutralCultures);

            return languages
                .Distinct()
                .Map(
                    lang => allCultures.Filter(
                        ci => string.Equals(ci.ThreeLetterISOLanguageName, lang, StringComparison.OrdinalIgnoreCase)))
                .Sequence()
                .Flatten()
                .ToList();
        }
    }
}
