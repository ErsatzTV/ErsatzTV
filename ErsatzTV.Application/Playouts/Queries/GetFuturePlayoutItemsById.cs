using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.Playouts.Queries
{
    public record GetFuturePlayoutItemsById(int PlayoutId, int PageNum, int PageSize) : IRequest<PagedPlayoutItemsViewModel>;
}
