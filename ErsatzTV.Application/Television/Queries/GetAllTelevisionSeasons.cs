using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.Television.Queries
{
    public record GetAllTelevisionSeasons : IRequest<List<TelevisionSeasonViewModel>>;
}
