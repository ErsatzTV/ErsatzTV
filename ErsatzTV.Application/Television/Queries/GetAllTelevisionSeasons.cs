using System.Collections.Generic;
using ErsatzTV.Application.MediaItems;
using MediatR;

namespace ErsatzTV.Application.Television.Queries
{
    public record GetAllTelevisionSeasons : IRequest<List<NamedMediaItemViewModel>>;
}
