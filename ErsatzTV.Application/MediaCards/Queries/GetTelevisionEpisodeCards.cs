using MediatR;

namespace ErsatzTV.Application.MediaCards.Queries
{
    public record GetTelevisionEpisodeCards
        (int TelevisionSeasonId, int PageNumber, int PageSize) : IRequest<TelevisionEpisodeCardResultsViewModel>;
}
