using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.Libraries.Queries
{
    public record GetLocalLibraries : IRequest<List<LocalLibraryViewModel>>;
}
