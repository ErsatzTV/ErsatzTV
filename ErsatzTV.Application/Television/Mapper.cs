using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ErsatzTV.Application.MediaCards;
using ErsatzTV.Core.Domain;
using LanguageExt;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.Television
{
    internal static class Mapper
    {
        internal static TelevisionShowViewModel ProjectToViewModel(
            Show show,
            List<string> languages,
            Option<JellyfinMediaSource> maybeJellyfin) =>
            new(
                show.Id,
                show.ShowMetadata.HeadOrNone().Map(m => m.Title ?? string.Empty).IfNone(string.Empty),
                show.ShowMetadata.HeadOrNone().Map(m => m.Year?.ToString() ?? string.Empty).IfNone(string.Empty),
                show.ShowMetadata.HeadOrNone().Map(m => m.Plot ?? string.Empty).IfNone(string.Empty),
                show.ShowMetadata.HeadOrNone().Map(m => GetPoster(m, maybeJellyfin)).IfNone(string.Empty),
                show.ShowMetadata.HeadOrNone().Map(m => GetFanArt(m, maybeJellyfin)).IfNone(string.Empty),
                show.ShowMetadata.HeadOrNone().Map(m => m.Genres.Map(g => g.Name).ToList()).IfNone(new List<string>()),
                show.ShowMetadata.HeadOrNone().Map(m => m.Tags.Map(g => g.Name).ToList()).IfNone(new List<string>()),
                show.ShowMetadata.HeadOrNone().Map(m => m.Studios.Map(s => s.Name).ToList())
                    .IfNone(new List<string>()),
                LanguagesForShow(languages),
                show.ShowMetadata.HeadOrNone()
                    .Map(
                        m => m.Actors.OrderBy(a => a.Order).ThenBy(a => a.Id)
                            .Map(a => MediaCards.Mapper.ProjectToViewModel(a, maybeJellyfin))
                            .ToList())
                    .IfNone(new List<ActorCardViewModel>()));

        internal static TelevisionSeasonViewModel ProjectToViewModel(
            Season season,
            Option<JellyfinMediaSource> maybeJellyfin) =>
            new(
                season.Id,
                season.ShowId,
                season.Show.ShowMetadata.HeadOrNone().Map(m => m.Title ?? string.Empty).IfNone(string.Empty),
                season.Show.ShowMetadata.HeadOrNone().Map(m => m.Year?.ToString() ?? string.Empty).IfNone(string.Empty),
                season.SeasonNumber == 0 ? "Specials" : $"Season {season.SeasonNumber}",
                season.SeasonMetadata.HeadOrNone().Map(m => GetPoster(m, maybeJellyfin)).IfNone(string.Empty),
                season.Show.ShowMetadata.HeadOrNone().Map(m => GetFanArt(m, maybeJellyfin)).IfNone(string.Empty));

        internal static TelevisionEpisodeViewModel ProjectToViewModel(Episode episode) =>
            new(
                episode.Season.ShowId,
                episode.SeasonId,
                episode.EpisodeNumber,
                episode.EpisodeMetadata.HeadOrNone().Map(m => m.Title ?? string.Empty).IfNone(string.Empty),
                episode.EpisodeMetadata.HeadOrNone().Map(m => m.Plot ?? string.Empty).IfNone(string.Empty),
                episode.EpisodeMetadata.HeadOrNone().Map(m => GetThumbnail(m, None)).IfNone(string.Empty));

        private static string GetPoster(Metadata metadata, Option<JellyfinMediaSource> maybeJellyfin) =>
            GetArtwork(metadata, ArtworkKind.Poster, maybeJellyfin);

        private static string GetFanArt(Metadata metadata, Option<JellyfinMediaSource> maybeJellyfin) =>
            GetArtwork(metadata, ArtworkKind.FanArt, maybeJellyfin);

        private static string GetThumbnail(Metadata metadata, Option<JellyfinMediaSource> maybeJellyfin) =>
            GetArtwork(metadata, ArtworkKind.Thumbnail, maybeJellyfin);

        private static string GetArtwork(
            Metadata metadata,
            ArtworkKind artworkKind,
            Option<JellyfinMediaSource> maybeJellyfin)
        {
            string artwork = Optional(metadata.Artwork.FirstOrDefault(a => a.ArtworkKind == artworkKind))
                .Match(a => a.Path, string.Empty);

            if (maybeJellyfin.IsSome && artwork.StartsWith("jellyfin://"))
            {
                string address = maybeJellyfin.Map(ms => ms.Connections.HeadOrNone().Map(c => c.Address))
                    .Flatten()
                    .IfNone("jellyfin://");
                artwork = artwork.Replace("jellyfin://", address);

                if (artworkKind == ArtworkKind.Poster)
                {
                    artwork += "&fillHeight=440";
                }
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
