using ErsatzTV.Core.Domain;
using MediatR;

namespace ErsatzTV.Application.MediaItems.Queries
{
    public record GetAggregateMediaItems
        (MediaType MediaType, int PageNumber, int PageSize) : IRequest<AggregateMediaItemResults>;
}
