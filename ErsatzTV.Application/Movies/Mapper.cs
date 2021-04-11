using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ErsatzTV.Core.Domain;
using LanguageExt;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.Movies
{
    internal static class Mapper
    {
        internal static MovieViewModel ProjectToViewModel(Movie movie)
        {
            MovieMetadata metadata = Optional(movie.MovieMetadata).Flatten().Head();
            return new MovieViewModel(
                metadata.Title,
                metadata.Year?.ToString(),
                metadata.Plot,
                Artwork(metadata, ArtworkKind.Poster),
                Artwork(metadata, ArtworkKind.FanArt),
                metadata.Genres.Map(g => g.Name).ToList(),
                metadata.Tags.Map(t => t.Name).ToList(),
                metadata.Studios.Map(s => s.Name).ToList(),
                LanguagesForMovie(movie));
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

        private static string Artwork(Metadata metadata, ArtworkKind artworkKind) =>
            Optional(metadata.Artwork.FirstOrDefault(a => a.ArtworkKind == artworkKind))
                .Match(a => a.Path, string.Empty);
    }
}
