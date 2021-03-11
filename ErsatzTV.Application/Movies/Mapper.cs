using System.Linq;
using ErsatzTV.Core.Domain;
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
                metadata.Genres.Map(g => g.Name).ToList());
        }

        private static string Artwork(Metadata metadata, ArtworkKind artworkKind) =>
            Optional(metadata.Artwork.FirstOrDefault(a => a.ArtworkKind == artworkKind))
                .Match(a => a.Path, string.Empty);
    }
}
