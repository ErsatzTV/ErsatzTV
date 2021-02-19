using ErsatzTV.Core.AggregateModels;
using MediatR;

namespace ErsatzTV.Application.MediaItems.Queries
{
    public record GetAggregateMediaItems
        (AggregateMediaItemType ItemType, int PageNumber, int PageSize) : IRequest<AggregateMediaItemResults>;
}
