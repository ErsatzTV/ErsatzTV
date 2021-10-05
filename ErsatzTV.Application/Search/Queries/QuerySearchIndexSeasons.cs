using ErsatzTV.Application.MediaCards;
using MediatR;

namespace ErsatzTV.Application.Search.Queries
{
    public record QuerySearchIndexSeasons
        (string Query, int PageNumber, int PageSize) : IRequest<TelevisionSeasonCardResultsViewModel>;
}
