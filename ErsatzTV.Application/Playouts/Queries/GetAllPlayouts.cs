using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.Playouts.Queries
{
    public record GetAllPlayouts : IRequest<List<PlayoutNameViewModel>>;
}
