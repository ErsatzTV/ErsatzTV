using MediatR;

namespace ErsatzTV.Application.MediaCards.Queries
{
    public record GetMovieCards(int PageNumber, int PageSize) : IRequest<MovieCardResultsViewModel>;
}
