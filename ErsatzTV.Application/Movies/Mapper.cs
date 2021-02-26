using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Movies
{
    internal static class Mapper
    {
        internal static MovieViewModel ProjectToViewModel(Movie movie) =>
            new(movie.Metadata.Title, movie.Metadata.Year?.ToString(), movie.Metadata.Plot, movie.Poster);
    }
}
