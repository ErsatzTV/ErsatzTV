using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.MediaItems.Queries
{
    public record GetAllMediaItems : IRequest<List<MediaItemViewModel>>;
}
