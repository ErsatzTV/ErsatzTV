using System.Collections.Generic;
using ErsatzTV.Application.MediaItems;
using MediatR;

namespace ErsatzTV.Application.Artists.Queries
{
    public record GetAllArtists : IRequest<List<NamedMediaItemViewModel>>;
}
