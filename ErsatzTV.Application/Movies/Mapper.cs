﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Emby;
using ErsatzTV.Core.Jellyfin;
using Flurl;
using LanguageExt;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.Movies
{
    internal static class Mapper
    {
        internal static MovieViewModel ProjectToViewModel(
            Movie movie,
            Option<JellyfinMediaSource> maybeJellyfin,
            Option<EmbyMediaSource> maybeEmby)
        {
            MovieMetadata metadata = Optional(movie.MovieMetadata).Flatten().Head();
            return new MovieViewModel(
                metadata.Title,
                metadata.Year?.ToString(),
                metadata.Plot,
                metadata.Genres.Map(g => g.Name).ToList(),
                metadata.Tags.Map(t => t.Name).ToList(),
                metadata.Studios.Map(s => s.Name).ToList(),
                LanguagesForMovie(movie),
                metadata.Actors.OrderBy(a => a.Order).ThenBy(a => a.Id)
                    .Map(a => MediaCards.Mapper.ProjectToViewModel(a, maybeJellyfin, maybeEmby))
                    .ToList())
            {
                Poster = Artwork(metadata, ArtworkKind.Poster, maybeJellyfin, maybeEmby),
                FanArt = Artwork(metadata, ArtworkKind.FanArt, maybeJellyfin, maybeEmby)
            };
        }

        private static List<CultureInfo> LanguagesForMovie(Movie movie)
        {
            CultureInfo[] allCultures = CultureInfo.GetCultures(CultureTypes.NeutralCultures);

            return movie.MediaVersions
                .Map(mv => mv.Streams.Filter(s => s.MediaStreamKind == MediaStreamKind.Audio).Map(s => s.Language))
                .Flatten()
                .Distinct()
                .Map(
                    lang => allCultures.Filter(
                        ci => string.Equals(ci.ThreeLetterISOLanguageName, lang, StringComparison.OrdinalIgnoreCase)))
                .Sequence()
                .Flatten()
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

            if (maybeJellyfin.IsSome && artwork.StartsWith("jellyfin://"))
            {
                Url url = JellyfinUrl.ForArtwork(maybeJellyfin, artwork);
                if (artworkKind is ArtworkKind.Poster or ArtworkKind.Thumbnail)
                {
                    url.SetQueryParam("fillHeight", 440);
                }

                artwork = url;
            }
            else if (maybeEmby.IsSome && artwork.StartsWith("emby://"))
            {
                Url url = EmbyUrl.ForArtwork(maybeEmby, artwork);
                if (artworkKind is ArtworkKind.Poster or ArtworkKind.Thumbnail)
                {
                    url.SetQueryParam("fillHeight", 440);
                }

                artwork = url;
            }

            return artwork;
        }
    }
}
