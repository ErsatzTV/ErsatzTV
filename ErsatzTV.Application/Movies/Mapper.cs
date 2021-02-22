using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Movies
{
    internal static class Mapper
    {
        internal static MovieViewModel ProjectToViewModel(MovieMediaItem movie) =>
            new(movie.Metadata.Title, movie.Metadata.Year?.ToString(), movie.Metadata.Plot, movie.Poster);
    }
}
