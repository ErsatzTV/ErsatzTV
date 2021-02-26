using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Movies
{
    internal static class Mapper
    {
        internal static MovieViewModel ProjectToViewModel(Movie movie)
        {
            string title = movie.MovieMetadata.HeadOrNone().Map(m => m.Title).IfNone(string.Empty);
            string year = movie.MovieMetadata.HeadOrNone()
                .Map(m => m.ReleaseDate?.Year.ToString())
                .IfNone(string.Empty);
            string plot = movie.MovieMetadata.HeadOrNone().Map(m => m.Plot).IfNone(string.Empty);

            return new MovieViewModel(title, year, plot, movie.Poster);
        }
    }
}
