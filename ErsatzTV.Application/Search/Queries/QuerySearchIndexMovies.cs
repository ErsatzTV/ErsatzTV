using ErsatzTV.Application.MediaCards;
using MediatR;

namespace ErsatzTV.Application.Search.Queries
{
    public record QuerySearchIndexMovies
        (string Query, int PageNumber, int PageSize) : IRequest<MovieCardResultsViewModel>;
}
