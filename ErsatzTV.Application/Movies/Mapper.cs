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
                metadata.ReleaseDate?.Year.ToString(),
                metadata.Plot,
                Optional(metadata.Artwork.FirstOrDefault(a => a.ArtworkKind == ArtworkKind.Poster))
                    .Match(a => a.Path, string.Empty));
        }
    }
}
