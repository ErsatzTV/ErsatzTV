using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.Playouts.Queries
{
    public record GetPlayoutItemsById(int PlayoutId, int PageNum, int PageSize) : IRequest<PagedPlayoutItemsViewModel>;
}
