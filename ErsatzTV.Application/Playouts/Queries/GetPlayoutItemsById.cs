using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.Playouts.Queries
{
    public record GetPlayoutItemsById(int PlayoutId) : IRequest<List<PlayoutItemViewModel>>;
}
