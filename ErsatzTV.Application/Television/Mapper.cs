﻿using System.Globalization;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Emby;
using ErsatzTV.Core.Jellyfin;
using Flurl;

namespace ErsatzTV.Application.Television;

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
            show.ShowMetadata.HeadOrNone().Map(m => m.Year?.ToString(CultureInfo.InvariantCulture) ?? string.Empty)
                .IfNone(string.Empty),
            show.ShowMetadata.HeadOrNone().Map(m => m.Plot ?? string.Empty).IfNone(string.Empty),
            show.ShowMetadata.HeadOrNone().Map(m => GetPoster(m, maybeJellyfin, maybeEmby)).IfNone(string.Empty),
            show.ShowMetadata.HeadOrNone().Map(m => GetFanArt(m, maybeJellyfin, maybeEmby)).IfNone(string.Empty),
            show.ShowMetadata.HeadOrNone().Map(m => m.Genres.Map(g => g.Name).ToList()).IfNone([]),
            show.ShowMetadata.HeadOrNone().Map(m =>
                m.Tags.Where(t => string.IsNullOrWhiteSpace(t.ExternalTypeId)).Map(g => g.Name).ToList()).IfNone([]),
            show.ShowMetadata.HeadOrNone().Map(m => m.Studios.Map(s => s.Name).ToList()).IfNone([]),
            show.ShowMetadata.HeadOrNone().Map(m =>
                m.Tags.Where(t => t.ExternalTypeId == Tag.PlexNetworkTypeId).Map(g => g.Name).ToList()).IfNone([]),
            show.ShowMetadata.HeadOrNone()
                .Map(m => (m.ContentRating ?? string.Empty).Split("/").Map(s => s.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x)).ToList()).IfNone([]),
            LanguagesForShow(languages),
            show.ShowMetadata.HeadOrNone()
                .Map(m => m.Actors.OrderBy(a => a.Order).ThenBy(a => a.Id)
                    .Map(a => MediaCards.Mapper.ProjectToViewModel(a, maybeJellyfin, maybeEmby))
                    .ToList())
                .IfNone([]));

    internal static TelevisionSeasonViewModel ProjectToViewModel(
        Season season,
        Option<JellyfinMediaSource> maybeJellyfin,
        Option<EmbyMediaSource> maybeEmby) =>
        new(
            season.Id,
            season.ShowId,
            season.Show.ShowMetadata.HeadOrNone().Map(m => m.Title ?? string.Empty).IfNone(string.Empty),
            season.Show.ShowMetadata.HeadOrNone()
                .Map(m => m.Year?.ToString(CultureInfo.InvariantCulture) ?? string.Empty).IfNone(string.Empty),
            season.SeasonNumber == 0 ? "Specials" : $"Season {season.SeasonNumber}",
            season.SeasonMetadata.HeadOrNone().Map(m => GetPoster(m, maybeJellyfin, maybeEmby))
                .IfNone(string.Empty),
            season.Show.ShowMetadata.HeadOrNone().Map(m => GetFanArt(m, maybeJellyfin, maybeEmby))
                .IfNone(string.Empty));

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

    private static string GetArtwork(
        Metadata metadata,
        ArtworkKind artworkKind,
        Option<JellyfinMediaSource> maybeJellyfin,
        Option<EmbyMediaSource> maybeEmby)
    {
        string artwork = Optional(metadata.Artwork.FirstOrDefault(a => a.ArtworkKind == artworkKind))
            .Match(a => a.Path, string.Empty);

        if (maybeJellyfin.IsSome && artwork.StartsWith("jellyfin://", StringComparison.OrdinalIgnoreCase))
        {
            Url url = JellyfinUrl.RelativeProxyForArtwork(artwork);
            if (artworkKind == ArtworkKind.Poster)
            {
                url.SetQueryParam("fillHeight", 440);
            }

            artwork = url;
        }
        else if (maybeEmby.IsSome && artwork.StartsWith("emby://", StringComparison.OrdinalIgnoreCase))
        {
            Url url = EmbyUrl.RelativeProxyForArtwork(artwork);
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
            .Map(lang => allCultures.Filter(ci => string.Equals(
                ci.ThreeLetterISOLanguageName,
                lang,
                StringComparison.OrdinalIgnoreCase)))
            .Flatten()
            .Distinct()
            .ToList();
    }
}
