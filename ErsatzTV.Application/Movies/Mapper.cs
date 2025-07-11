﻿using System.Globalization;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Emby;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Jellyfin;
using Flurl;

namespace ErsatzTV.Application.Movies;

internal static class Mapper
{
    internal static MovieViewModel ProjectToViewModel(
        Movie movie,
        string localPath,
        List<string> languageCodes,
        Option<JellyfinMediaSource> maybeJellyfin,
        Option<EmbyMediaSource> maybeEmby)
    {
        MovieMetadata metadata = Optional(movie.MovieMetadata).Flatten().Head();
        return new MovieViewModel(
            metadata.Title,
            metadata.Year?.ToString(CultureInfo.InvariantCulture),
            metadata.Plot,
            metadata.Genres.Map(g => g.Name).ToList(),
            metadata.Tags.Map(t => t.Name).ToList(),
            metadata.Studios.Map(s => s.Name).ToList(),
            (metadata.ContentRating ?? string.Empty).Split("/").Map(s => s.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x)).ToList(),
            LanguagesForMovie(languageCodes),
            metadata.Actors.OrderBy(a => a.Order).ThenBy(a => a.Id)
                .Map(a => MediaCards.Mapper.ProjectToViewModel(a, maybeJellyfin, maybeEmby))
                .ToList(),
            metadata.Directors.Map(d => d.Name).ToList(),
            metadata.Writers.Map(w => w.Name).ToList(),
            movie.GetHeadVersion().MediaFiles.Head().Path,
            localPath,
            movie.State)
        {
            Poster = Artwork(metadata, ArtworkKind.Poster, maybeJellyfin, maybeEmby),
            FanArt = Artwork(metadata, ArtworkKind.FanArt, maybeJellyfin, maybeEmby)
        };
    }

    private static List<string> LanguagesForMovie(List<string> languageCodes)
    {
        CultureInfo[] allCultures = CultureInfo.GetCultures(CultureTypes.NeutralCultures);

        return languageCodes
            .Map(lang => allCultures.Filter(ci => string.Equals(
                ci.ThreeLetterISOLanguageName,
                lang,
                StringComparison.OrdinalIgnoreCase)))
            .Flatten()
            .Map(ci => ci.EnglishName)
            .Distinct()
            .ToList();
    }

    private static string Artwork(
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
            if (artworkKind is ArtworkKind.Poster or ArtworkKind.Thumbnail)
            {
                url.SetQueryParam("fillHeight", 440);
            }

            artwork = url;
        }
        else if (maybeEmby.IsSome && artwork.StartsWith("emby://", StringComparison.OrdinalIgnoreCase))
        {
            Url url = EmbyUrl.RelativeProxyForArtwork(artwork);
            if (artworkKind is ArtworkKind.Poster or ArtworkKind.Thumbnail)
            {
                url.SetQueryParam("maxHeight", 440);
            }

            artwork = url;
        }

        return artwork;
    }
}
