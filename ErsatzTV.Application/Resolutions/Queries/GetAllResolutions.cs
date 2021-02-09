using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.Resolutions.Queries
{
    public record GetAllResolutions : IRequest<List<ResolutionViewModel>>;
}
