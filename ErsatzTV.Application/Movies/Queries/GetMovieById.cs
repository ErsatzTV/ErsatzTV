using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Movies.Queries
{
    public record GetMovieById(int Id) : IRequest<Option<MovieViewModel>>;
}
