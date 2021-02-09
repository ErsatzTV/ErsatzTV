using System.Collections.Generic;
using ErsatzTV.Core.Domain;
using MediatR;

namespace ErsatzTV.Application.MediaItems.Queries
{
    public record GetAggregateMediaItems
        (MediaType MediaType, string SearchString) : IRequest<List<AggregateMediaItemViewModel>>;
}
