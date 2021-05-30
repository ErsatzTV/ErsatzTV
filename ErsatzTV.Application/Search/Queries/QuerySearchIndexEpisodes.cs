using ErsatzTV.Application.MediaCards;
using MediatR;

namespace ErsatzTV.Application.Search.Queries
{
    public record QuerySearchIndexEpisodes
        (string Query, int PageNumber, int PageSize) : IRequest<TelevisionEpisodeCardResultsViewModel>;
}
