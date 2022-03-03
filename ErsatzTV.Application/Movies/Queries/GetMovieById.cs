namespace ErsatzTV.Application.Movies;

public record GetMovieById(int Id) : IRequest<Option<MovieViewModel>>;